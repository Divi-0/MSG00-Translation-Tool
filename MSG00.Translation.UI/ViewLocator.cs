using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MSG00.Translation.UI.ViewModels;
using System;
using System.Collections.Generic;

namespace MSG00.Translation.UI
{
    public class ViewLocator : IDataTemplate
    {
        Dictionary<string, Control> views = new Dictionary<string, Control>();

        public Control Build(object data)
        {
            if (data is null)
                return null;

            string name = data.GetType().FullName!.Replace("ViewModel", "View");

            if (!views.ContainsKey(name))
            {
                var type = Type.GetType(name);

                ArgumentNullException.ThrowIfNull(type);

                views.Add(name, (Control)Activator.CreateInstance(type)!);
            }

            return views[name];
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}