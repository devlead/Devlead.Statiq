using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Devlead.Statiq.Tabs
{
    public class TabGroup
    {
        public string Id { get; } = Guid.NewGuid().ToString("n");
        public TabGroupTab[] Tabs { get; set; } 
    }
}
