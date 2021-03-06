﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkZoneLoad.Models
{
    public class ResponseOFSC
    {
        public int statusCode { get; set; }
        public string Content { get; set; }
        public string ErrorMessage { get; set; }
    }

    // public class Item
   
    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

    public class RootObject
    {
        public List<WorkZone> items { get; set; }
        public int totalResults { get; set; }
        public List<Link> links { get; set; }
    }
}
