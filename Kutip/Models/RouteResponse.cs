using Microsoft.AspNetCore.Mvc;

namespace Kutip.Models
{
    public class RouteResponse : Controller
    {

        public Route[] Routes { get; set; }


        public class Route
        {
            public Summary Summary { get; set; }
            public Geometry Geometry { get; set; }
        }

        public class Summary
        {
            public int Distance { get; set; } // meters
            public int Duration { get; set; } // seconds
        }

        public class Geometry
        {
            public double[][] Coordinates { get; set; } // [lat, lon] pairs
        }
    }
}