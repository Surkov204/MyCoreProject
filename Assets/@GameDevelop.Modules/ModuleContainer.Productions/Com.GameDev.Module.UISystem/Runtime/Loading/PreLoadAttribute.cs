using System;

namespace Com.GameDev.Module.UISystem
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PreLoadAttribute : Attribute
    {
        public bool ShowLoading { get; }

        public PreLoadAttribute(bool showLoading = true)
        {
            ShowLoading = showLoading;
        }
    }
}