using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkZoneLoad.Controller;
using WorkZoneLoad.Models;

namespace WorkZoneLoad
{
    public enum LoggerOpcion
    {
        OK = 1,
        ERROR = 2,
        LOG = 0,
        BORRO = 3
    }

    public enum HttpStatus
    {
        OK = 200,
        BADREQUEST = 400
    }
    public class Program
    {
        private static List<WorkZone> listWorkZone = new List<WorkZone>();
        private static List<WorkZone> listWorkZoneActive = new List<WorkZone>();
        private static List<WorkZone> listWorkZoneInactive = new List<WorkZone>();

        public static List<string> list { get; set; } = new List<string>();
        public static List<Resource> listResource { get; set; }
        public static string sPath { get; set; } = @"C:\Users\inmotion\Documents\bitbucket\ofsc\skymx\code\html";
        static void Main(string[] args)
        {
            Console.WriteLine(" Ingrese Ubicaci�n (Folder) en donde buscar archivos");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Por ejemplo C:\\Users\\inmotion\\Documents\\z");
            Console.ResetColor();
            sPath = Console.ReadLine();

            if (Directory.Exists(sPath))
                Console.WriteLine("Leyendo archivos CSV");
            else
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("Ubicaci�n no valida {0}", sPath));
                Thread.Sleep(1800);
                Console.ResetColor();
                throw new Exception(" x=> { x.id = 'error' }");
            }
            //ReadCSV(sPath);
            //// Fill Object
            //Split();
            Console.WriteLine("Obteniendo Zonas de Trabajo disponibles " + DateTime.Now);
            // *****************************************************************
            // Get all workzone
            WorkZoneController ctrlWorkZone = new WorkZoneController();
            listWorkZone = ctrlWorkZone.GetAll();
            Console.WriteLine("Termino de obtener Zonas de Trabajo disponibles " + listWorkZone.Count() + "  " + DateTime.Now);
            // listWorkZoneInactive = (listWorkZone.Where(x => x.status == "inactive").ToList());
            listWorkZoneActive = listWorkZone.Where(x => x.status == "active").ToList();
            // end
            // *****************************************************************
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.Clear();
            Logger("----------------------------------------------------------------------------------");
            string token = ConfigurationManager.AppSettings["execute"];

            Logger("Inicio del proceso para instancia con el token " + token);
            Logger(DateTime.Now.ToString());
            Console.WriteLine("Leyendo archivos CSV");

            ReadCSV(sPath);
            // Fill Object
            Split();
            Console.WriteLine("Se asignaran zonas de trabajo a " + listResource.Count + " recursos");
            int addworkzone = 0;
            int replaceworkzone = 0;
            foreach (var resource in listResource)
            {
                // Obtiene zonas de trabajo de recurso
                WorkZoneController workZoneController = new WorkZoneController();
                Console.Clear();
                Console.WriteLine("---------------------------------------------------");
                Console.WriteLine(" Recurso " + resource.externalId);

                List<WorkZone> listWorkZonesCurrent = new List<WorkZone>();
                List<string> listWorkZonesCSV = workZoneController.Ranges(resource.workZone);

                if (listWorkZonesCSV.Count > 0)
                {
                    List<WorkZone> listworkZone = new List<WorkZone>();
                    List<string> listTmpWorkZoneCSV = new List<string>();

                    foreach (var item in listWorkZonesCSV)
                    {
                        if (listWorkZoneActive.Exists(x => x.workZoneLabel == item))
                            listTmpWorkZoneCSV.Add(item);
                        else
                            continue;
                    }

                    listWorkZonesCurrent = workZoneController.Get(resource.externalId);
                    List<string> listWorkZoneCurrentTmp = listWorkZonesCurrent.Select(x => x.workZone).ToList();
                    List<string> listWorkZoneCurrentTmpDelete = listWorkZonesCurrent.Where(x => x.endDate >= DateTime.Now.AddMonths(-6)).Select(x => x.workZone).ToList();
                    var deleteItems = listWorkZoneCurrentTmpDelete.Except(listTmpWorkZoneCSV).ToList();
                    var addItems = listTmpWorkZoneCSV.Except(listWorkZoneCurrentTmp).ToList();

                    // delete item
                    if (resource.workZone.replace)
                    {
                        Console.WriteLine("Reemplazando zonas de trabajo del recurso {0} ", resource.externalId);

                        foreach (var itemZipCode in deleteItems)
                        {
                            var tmp = listWorkZonesCurrent.FirstOrDefault(x => x.workZone == itemZipCode);
                            if (workZoneController.Delete(tmp, resource.externalId))
                            {
                                replaceworkzone = replaceworkzone + 1;
                                Console.WriteLine(string.Format("* Se borro correctamente del recurso {0} el c�digo postal {1}", resource.externalId, itemZipCode));

                            }
                            else
                                Console.WriteLine(string.Format("* No logro borrar del recurso {0} el c�digo postal {1}", resource.externalId, itemZipCode));
                        }
                    }

                    // add item
                    Console.WriteLine("Agregando zonas de trabajo del recurso {0} ", resource.externalId);
                    foreach (string itemWorkZoneAdd in addItems)
                    {
                        if (workZoneController.Add(resource.externalId, itemWorkZoneAdd))
                        {
                            Console.WriteLine(string.Format("* Se agrego correctamente al recurso {0} el c�digo postal {1}", resource.externalId, itemWorkZoneAdd));
                            addworkzone = addworkzone + 1;
                        }

                        //else
                        //    Console.WriteLine(string.Format("* No logro asignar del recurso {0} el c�digo postal {1}", resource.externalId, itemWorkZoneAdd));
                    }

                }
            }
            // multitask 
            stopwatch.Stop();
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("Ha terminado de asignar a {0} recursos ", listResource.Count);
            Console.WriteLine("Total de asignaciones realizadas {0} ", addworkzone);
            Console.WriteLine("Total de asignaciones reemplazadas {0} ", replaceworkzone);
            Console.WriteLine("Se tardo en Milisegundos " + stopwatch.Elapsed.TotalMilliseconds);
            Console.WriteLine("Se tardo en Segundos " + stopwatch.Elapsed.TotalSeconds);
            Console.WriteLine("Se tardo en Minutos " + stopwatch.Elapsed.TotalMinutes);
            Logger("Milisegundos " + stopwatch.Elapsed.TotalMilliseconds.ToString());
            Logger("Segundos " + stopwatch.Elapsed.TotalSeconds.ToString());
            Logger("Minutos " + stopwatch.Elapsed.TotalMinutes.ToString());
            Logger(DateTime.Now.ToString());
            Logger(" End ");
            Logger("----------------------------------------------------------------------------------");
            Console.ReadLine();
        }
        static Resource FillObject(string[] aItems)
        {
            Resource objResource = new Resource();
            WorkZoneController workZone = new WorkZoneController();
            objResource.workZone = new WorkZone();
            try
            {
                objResource.parentId = aItems[0].Trim();
                objResource.externalId = aItems[1].Trim();
                objResource.resourceType = aItems[3].Trim();

                if (aItems.Count() >= 25)
                {
                    // AGREGAR
                    if (string.IsNullOrEmpty(aItems[23]) || aItems[23].ToString().ToUpper() == "AGREGAR")
                        objResource.workZone.replace = false;
                    else
                        // REEMPLAZAR
                        objResource.workZone.replace = true;

                    if (string.IsNullOrEmpty(aItems[24]))
                        return null;
                    else
                    {
                        objResource.resource_workzones = aItems[25];

                        switch (aItems[24])
                        {
                            case "MX":
                                objResource.resource_workzones = aItems[25];
                                break;

                            case "CR":
                                objResource.workZone.country = "CR";
                                objResource.resource_workzones = aItems[25];
                                break;

                            case "GT":
                                objResource.workZone.country = "GT";
                                objResource.resource_workzones = aItems[25];
                                break;

                            case "HN":
                                objResource.workZone.country = "HN";
                                objResource.resource_workzones = aItems[25];
                                break;

                            case "NI":
                                objResource.workZone.country = "NI";
                                objResource.resource_workzones = aItems[25];
                                break;

                            case "PA":
                                objResource.workZone.country = "PA";
                                objResource.resource_workzones = aItems[25];
                                break;

                            case "SV":
                                objResource.workZone.country = "SV";
                                objResource.resource_workzones = aItems[25];
                                break;

                            default:
                                objResource.resource_workzones = aItems[25];
                                break;
                        }
                    }
                }

                else
                    return null;

                objResource.workZone.source = objResource.resource_workzones;
                objResource.workZone.id = workZone.Ranges(objResource.workZone);

            }
            catch (Exception ex)
            {
                string text = string.Concat("* dont marred item parent id {0} and external id {1} no working but continue process, details: {2}", objResource.externalId, objResource.parentId, ex.Message);
                Console.WriteLine(text);
                Logger(text, LoggerOpcion.ERROR);
                return null;
            }
            return objResource;
        }
        private static void ReadCSV(string path)
        {
            Console.Clear();
            var files = Directory.GetFiles(path, "*.csv");

            foreach (var item in files)
            {
                try
                {
                    CSVController objCSVController = new CSVController();
                    objCSVController.source = @item;

                    Task<List<string>> task = objCSVController.LinesFile();
                    task.Wait();
                    var result = task.Result;
                    list.AddRange(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error  al leer el archivo {0} : Exepci�n :{1}", item, ex.Message));
                    Logger(string.Format("Error  al leer el archivo {0} : Exepci�n :{1}", item, ex.Message));
                    throw;
                }
            }
        }

        static void Split()
        {
            listResource = new List<Resource>();
            foreach (var item in list)
            {
                CSVController objCSVController = new CSVController();
                Resource objResource = new Resource();
                string[] result = objCSVController.SplitBy(item, ';');
                objResource = FillObject(result);
                if (objResource != null)
                {
                    //   objResource.resource_workzones
                    if (!string.IsNullOrEmpty(objResource.resource_workzones))
                        listResource.Add(objResource);
                }
            }
        }
        static string WorkZoneQueue(List<WorkZone> listworkZone)
        {
            int good = 0;
            int bad = 0;
            int limitTemp = 0;

            foreach (var item in listworkZone)
            {
                limitTemp++;
                var flag = WorkZoneMain(item);
                if (flag)
                    good++;
                else
                {
                    Logger(string.Format("workzone {0}|{1}|{2}|{3}|{4}|", item.workZoneLabel, item.status, item.travelArea, item.workZoneName, item.label.FirstOrDefault()));
                    bad++;
                }

                if (limitTemp == 1000)
                {
                    Thread.Sleep(1000);
                    limitTemp = 0;
                }
            }

            return string.Concat(listworkZone.Count, ",", good, ",", bad);
        }
        static bool WorkZoneMain(WorkZone workZone)
        {
            bool flag = false;
            WorkZoneController ctrlworkZone = new WorkZoneController();
            var checkExist = ctrlworkZone.Exist(workZone);
            if (checkExist)
                flag = ctrlworkZone.Set(workZone);
            else
                flag = ctrlworkZone.Create(workZone);

            return flag;
        }

        static void WorkZoneList(string externalId, List<string> list)
        {
            WorkZoneController workZoneController = new WorkZoneController();
            foreach (var item in list)
                workZoneController.Add(externalId, item);
        }



        public static void Logger(String lines, LoggerOpcion loggerOpcion = LoggerOpcion.LOG)
        {
            string temppath = string.Empty;
            try
            {
                switch ((int)loggerOpcion)
                {
                    case 1:
                        temppath = @sPath + "\\log_ok.txt";
                        break;
                    case 2:
                        // temppath = @sPath + "\\log_not.txt";
                        // break;
                        temppath = @sPath + "\\log_error.txt";
                        break;
                    case 3:
                        temppath = @sPath + "\\log_reemplar.txt";
                        break;
                    default:
                        temppath = @sPath + "\\log.txt";
                        break;
                }
                System.IO.StreamWriter file = new System.IO.StreamWriter(temppath, true);
                file.WriteLine(DateTime.Now + " : " + lines);
                file.Close();
            }
            catch
            {
                Thread.Sleep(800);
                Logger(lines, loggerOpcion);
            }
        }

    }
}

// end 