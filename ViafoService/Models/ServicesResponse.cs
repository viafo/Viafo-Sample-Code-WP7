using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace ViafoService.Models
{
    public class ServicesResponse
    {
        public List<Service> Services { get; set; }

        public string Url { get; set; }

        public int Code { get; set; }

        public List<string> Verbs { get; set; }

        /*
        {
         "services": [
            ...
         ], 
         "url": "https://vsg-test.appspot.com/client/1/get_services.html?access_token=K5LTIKRwILrTOLNKsZJgC6wHirtTb8DD9qYg2IB5gzXr96yjUABLxY9J5ve9ZKE6", 
         "code": 200, 
         "verbs": [
          "share", 
          "feed"
         ]
        }
        */
    }
}
