using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Linq;
using System.ComponentModel;
using System.Security.Policy;

namespace NINI
{

    /// <summary>
    /// 窗体管理器
    /// </summary>
    public static class WindowManager
    {
        // 窗体缓存
        static List<Window> FixedSingleFormCache = new List<Window>();

        #region 公开方法

        /// <summary>
        /// 获取一个与指定条件匹配的窗体对象，如果未找到默认会创建一个
        /// </summary>
        /// <typeparam name="TWindow">窗体类型</typeparam>
        /// <param name="createIfNotFound">如果主动创建</param>
        /// <param name="keySelector">要查找的条件表达式</param>
        /// <param name="parameters">实例化的参数</param>
        /// <returns></returns>
        public static TWindow Single<TWindow>(Func<TWindow, bool> keySelector = null, params object[] parameters) where TWindow : Window, new()
        {
            TWindow instance = null;

            try
            {
                instance = Get<TWindow>(keySelector);

                if (instance == null)
                {
                    instance = CreateInstance<TWindow>(parameters);
                    instance.Closing += delegate (object sender, CancelEventArgs e)
                    {
                        e.Cancel = true;
                        instance.Hide();
                    };
                    instance.Closed += delegate (object sender, EventArgs e)
                    {
                        FixedSingleFormCache.Remove(instance);
                        instance = null;
                    };
                    FixedSingleFormCache.Add(instance);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return instance;
        }


        /// <summary>
        /// 从缓存中取
        /// </summary>
        /// <typeparam name="TWindow"></typeparam>
        /// <param name="createIfNotFound"></param>
        /// <param name="keySelector"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static TWindow Cache<TWindow>(Func<TWindow, bool> keySelector = null, params object[] parameters) where TWindow : Window, new()
        {
            TWindow instance = null;

            try
            {
                instance = Get<TWindow>(keySelector);

                if (instance == null)
                {
                    instance = CreateInstance<TWindow>(parameters);

                    instance.Closed += delegate (object sender, EventArgs e)
                    {
                        FixedSingleFormCache.Remove(instance);
                        instance = null;
                    };
                    FixedSingleFormCache.Add(instance);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return instance;
        }


        /// <summary>
        /// 获取指定控件下的子控件集合中与指定条件匹配的窗体对象
        /// </summary>
        /// <typeparam name="TWindow">要查询的窗体类型</typeparam>
        /// <param name="parent">作为父窗口的控件</param>
        /// <param name="keySelector">查询条件</param>
        /// <returns></returns>
        public static TWindow Get<TWindow>(Func<TWindow, bool> keySelector = null) where TWindow : Window, new()
        {
            TWindow instance = null;

            foreach (TWindow form in FixedSingleFormCache)
            {
                TWindow comparingTarget = form as TWindow;
                if (keySelector != null)
                {
                    if (keySelector(comparingTarget))
                        instance = comparingTarget;
                    else
                        continue;
                }
                else
                {
                    if (comparingTarget != null)
                        instance = comparingTarget;
                    else
                        continue;
                }
                break;
            }

            return instance;
        }


        /// <summary>
        /// 
        /// </summary>
        public static void CloseAll()
        {
            for (int i = FixedSingleFormCache.Count - 1; i >= 0; i--)
            {
                var instance = FixedSingleFormCache[i];

                //ClearEvents(instance, "FormClsing");
                //instance.Close();
                if (instance != null)
                {
                    instance.ClearEvents();
                    FixedSingleFormCache.Remove(instance);
                    instance = null;
                }
            }
            FixedSingleFormCache.Clear();
        }

        #endregion

        static Delegate[] GetClosingEventInvocationList(Window target)
        {
            Delegate[] PaintEventInvocationList = null;

            var ParenTWindow = target;
            if (ParenTWindow == null)
                return PaintEventInvocationList;

            string PaintEventKeyName = "EventClosing";

            PropertyInfo FormEventProperty; // 窗体事件属性信息
            EventHandlerList FormEventPropertyInstance; // 窗体事件属性对象
            FieldInfo PaintEventKeyField;   // Paint事件对应的键值字段信息
            Object PaintEventKeyInstance; // Paint事件对应的键值对象
            Delegate PaintEventDelegate;

            Type FormType = typeof(System.Windows.Controls.Control);
            Type BaeTypeOfPaintEvent = typeof(System.Windows.Controls.Control);


            FormEventProperty = FormType.GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
            FormEventPropertyInstance = FormEventProperty.GetValue(ParenTWindow, null) as EventHandlerList;

            PaintEventKeyField = BaeTypeOfPaintEvent.GetField(PaintEventKeyName, BindingFlags.Static | BindingFlags.NonPublic);

            PaintEventKeyInstance = PaintEventKeyField.GetValue(ParenTWindow);
            PaintEventDelegate = FormEventPropertyInstance[PaintEventKeyInstance];

            if (PaintEventDelegate != null)
                PaintEventInvocationList = PaintEventDelegate.GetInvocationList();

            return PaintEventInvocationList;
        }

        static void ClearEvents(this object ctrl, string eventName = "_EventAll")
        {
            if (ctrl == null) return;
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Static;
            EventInfo[] events = ctrl.GetType().GetEvents(bindingFlags);
            if (events == null || events.Length < 1) return;

            for (int i = 0; i < events.Length; i++)
            {
                try
                {
                    EventInfo ei = events[i];
                    //只删除指定的方法，默认是_EventAll，前面加_是为了和系统的区分，防以后雷同
                    //if (eventName != "_EventAll" && ei.Name != eventName) continue;

                    /********************************************************
                     * class的每个event都对应了一个同名(变了，前面加了Event前缀)的private的delegate类
                     * 型成员变量（这点可以用Reflector证实）。因为private成
                     * 员变量无法在基类中进行修改，所以为了能够拿到base 
                     * class中声明的事件，要从EventInfo的DeclaringType来获取
                     * event对应的成员变量的FieldInfo并进行修改
                     ********************************************************/
                    FieldInfo fi = ei.DeclaringType.GetField("Event" + ei.Name, bindingFlags);
                    if (fi != null)
                    {
                        // 将event对应的字段设置成null即可清除所有挂钩在该event上的delegate
                        fi.SetValue(ctrl, null);
                    }
                }
                catch { }
            }
        }


        static TWindow CreateInstance<TWindow>(object[] parameters) where TWindow : Window, new()
        {
            TWindow instance = null;
            if (parameters?.Length > 0)
            {
                instance = Activator.CreateInstance(typeof(TWindow), parameters) as TWindow;
            }
            else
            {
                instance = new TWindow();
            }

            return instance;
        }


    }
}
