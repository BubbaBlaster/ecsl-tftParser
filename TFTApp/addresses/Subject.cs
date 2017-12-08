using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace Asteria.Utilities
{
    public static class Invoker
    {
        public static void Invoke(Delegate[] dList, object o, EventArgs args)
        {
            if (dList != null && dList.Count() > 0)
            {
                var param = new object[] { o, args };
                foreach (Delegate d in dList)
                {
                    if (d.Target is System.Windows.Forms.Control)
                    {
                        System.Windows.Forms.Control c = d.Target as System.Windows.Forms.Control;
                        if (c.InvokeRequired)
                            c.BeginInvoke(d, param);
                        else
                            d.DynamicInvoke(param);
                    }
                    else
                        d.DynamicInvoke(param);
                }
            }
        }
    }

    public class Subject : INotifyPropertyChanged
    {
        static protected Dictionary<string, object> NamedObjects = new Dictionary<string, object>();

        static public bool Contains<T>(string value)
        {
            if (!NamedObjects.Keys.Contains(value)) return false;
            if (NamedObjects[value].GetType() == typeof(T)) return true;
            return false;
        }

        static public Subject GetSubject(string tagName)
        {
            if (NamedObjects.Keys.Contains(tagName))
                return NamedObjects[tagName] as Subject;
            return null;
        }

        static public X Get<X>(string tagName)
            where X : class, new()
        {
            if (NamedObjects.Keys.Contains(tagName))
            {
                X ret1 = NamedObjects[tagName] as X;
                if (ret1 == null)
                    throw new Exception("Subject Key/Type Clash: The cast is not valid. <" + tagName + "> is stored as " + NamedObjects[tagName].ToString() + ".");
                return ret1;
            }

            X ret = new X();
            Subject.NamedObjects.Add(tagName, ret);
            return ret;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        virtual public void Dispose()
        { }

        public virtual void OnChanged()
        {
            if (PropertyChanged != null)
                Invoker.Invoke(PropertyChanged.GetInvocationList(), this, new PropertyChangedEventArgs(""));
        }
    }

    public class Event
    {
        static protected Dictionary<string, Event> NamedObjects = new Dictionary<string, Event>();

        static public Event Get(string tagName)
        {
            if (NamedObjects.Keys.Contains(tagName))
            {
                Event ret1 = NamedObjects[tagName];
                return ret1;
            }

            Event ret = new Event();
            NamedObjects.Add(tagName, ret);
            return ret;
        }

        public event PropertyChangedEventHandler RaiseEvent;

        public void Fire()
        {
            if (RaiseEvent != null)
                Invoker.Invoke(RaiseEvent.GetInvocationList(), this, new PropertyChangedEventArgs(""));
        }
    }

    public class ObservableString : Subject 
    {
        static public bool Contains(string tagName)
        {
            return Subject.Contains<ObservableString>(tagName);
        }

        static public ObservableString Get(string tagName)
        {
            return Get<ObservableString>(tagName);
        }

        private string _val = "";

        public ObservableString() {}

        public string Value
        {
            get { return _val; }
            set
            {
                if (_val == null)
                    _val = value;
                else
                {
                    if (_val.Equals(value)) return;
                    _val = value;
                }
                OnChanged();
            }
        }
    }

    public class Observable<T>: Subject
    {
        T _rawValObject;

        static public bool Contains(string tagName)
        {
            return Subject.Contains<Observable<T>>(tagName);
        }

        static public Observable<T> Get(string tagName )
        {
            return Subject.Get<Observable<T>>(tagName);
        }
        
        public Observable() { _rawValObject = default(T); }

        public T Value
        {
            get { return _rawValObject; }
            set
            {
                if (_rawValObject != null && _rawValObject.Equals(value)) return;
                _rawValObject = value;

                OnChanged();
            }
        }
    }
}

