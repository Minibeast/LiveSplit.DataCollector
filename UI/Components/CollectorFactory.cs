﻿using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;

[assembly: ComponentFactory(typeof(CollectorFactory))]

namespace LiveSplit.UI.Components
{
    public class CollectorFactory : IComponentFactory
    {
        public string ComponentName => "Data Collector";

        public string Description => "Sends LiveSplit data to server. (Made by Mini)";

        public ComponentCategory Category => ComponentCategory.Information;

        public IComponent Create(LiveSplitState state) => new CollectorComponent(state);

        public string UpdateName => ComponentName;

        public string XMLURL => "";

        public string UpdateURL => "";

        public Version Version => Version.Parse("1.0.0");
    }
}