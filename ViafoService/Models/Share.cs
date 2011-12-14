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
    public class Share
    {
        public string Service { get; set; }

        public string Text { get; set; }

        public string Link { get; set; }

        public string Id { get; set; }

        public event Action<string> OnSuccess;
        public event Action<string> OnFailure;

        internal void CallSilentFail()
        {
            if (this.OnFailure != null)
            {
                this.OnFailure(null);
            }
        }

        internal void CallSuccess()
        {
            if (this.OnSuccess != null)
            {
                this.OnSuccess(this.Service);
            }
        }

        internal void CallFailure()
        {
            if (this.OnFailure != null)
            {
                this.OnFailure(this.Service);
            }
        }
    }
}
