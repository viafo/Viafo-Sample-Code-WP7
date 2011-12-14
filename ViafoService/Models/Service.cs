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

namespace ViafoService.Models
{
    public class Service
    {
        public bool Authenticated { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string DisplayName { get; set; }

        public string Type { get; set; }

        /*
            {
            "authenticated": false, 
            "name": "identica", 
            "url": "https://vsg-test.appspot.com/auth/1/identica?access_token=K5LTIKRwILrTOLNKsZJgC6wHirtTb8DD9qYg2IB5gzXr96yjUABLxY9J5ve9ZKE6&return_url=http%3A%2F%2Fdev.viafo.com%2Ftest%2Fkittens%2F", 
            "params": {
            "lat": false, 
            "text": 132, 
            "link": false
            }, 
            "display_name": "identica", 
            "type": "share"
            }, 
         */
    }
}
