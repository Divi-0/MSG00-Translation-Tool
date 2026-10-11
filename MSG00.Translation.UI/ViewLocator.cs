using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MSG00.Translation.UI.ViewModels;
using System;
using System.Collections.Generic;

namespace MSG00.Translation.UI
{
    public class ViewLocator : IDataTemplate
    {
        /// <summary>
        /// Creates a new view per view model; a cached view instance can not be shown again for a second view model
        /// of the same type (e.g. opening C1_002 after C1_001), it is still attached to the previous content.
        /// </summary>
        public Control? Build(object? data)
        {
            if (data is null)
                return null;

            string name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            ArgumentNullException.ThrowIfNull(type);

            return (Control)Activator.CreateInstance(type)!;
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}