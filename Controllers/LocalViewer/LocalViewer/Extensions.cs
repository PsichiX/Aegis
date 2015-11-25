using System;
using System.Windows.Forms;

namespace LocalViewer
{
    public static class Extensions
    {
        public static void DoOnUiThread(this Control @this, Action code)
        {
            if (@this.InvokeRequired)
            {
                @this.BeginInvoke(code);
            }
            else
            {
                code.Invoke();
            }
        }
    }
}
