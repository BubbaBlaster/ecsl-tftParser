using System.Reflection;

namespace System.ComponentModel
{
    public static class ComponentModelExtension
    {
        public static void InvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action)
        {
            if (obj.InvokeRequired)
            {
                var args = new object[0];
                try
                {
                    obj.Invoke(action, args);
                }
                catch
                { // burn it... the only exception from here is caused by the main thread being closed
                }
            }
            else
            {
                action();
            }
        }
    }
}

namespace System.Windows.Forms
{
    public static class ControlExtension
    { 
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo?.SetValue(control, enable, null);
        }
    }
}
