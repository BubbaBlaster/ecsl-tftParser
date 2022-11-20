using Agora.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace Agora.Forms
{
    /// <summary>
    /// This class is here to override the Agora.Utilities.Subject.Invoker
    /// </summary>
    public class Invoker : IInvoker
    {
        public static void Configure()
        {
            Subject.Invoker = new Invoker();
        }

        [ExcludeFromCodeCoverage]
        private Invoker() { }

        [ExcludeFromCodeCoverage]
        public void Invoke(Delegate[] dList, object o, EventArgs args)
        {
            if (dList != null && dList.Length > 0)
            {
                var param = new object[] { o, args };

                foreach (Delegate d in dList)
                {
                    Control? c = d.Target as Control;
                    if (c?.InvokeRequired ?? false)
                        c.BeginInvoke(d, param);
                    else
                        d.DynamicInvoke(param);
                }
            }
        }
    }

}
