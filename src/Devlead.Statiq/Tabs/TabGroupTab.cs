using System;
using System.IO;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Devlead.Statiq.Tabs
{
    public class TabGroupTab
    {
        private string _name;
        public string Id { get; } = Guid.NewGuid().ToString("n");

        public string Name
        {
            get => _name ??= !string.IsNullOrWhiteSpace(Code) ? Path.GetFileName(Code) : Id;
            set => _name = value;
        }

        public string Content { get; set; }
        
        public string Code { get; set; }
        
        public string CodeLang { get; set; }
    }
}