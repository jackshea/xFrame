﻿namespace Framework.Runtime.Infrastructure.Notifications
{
    public interface ISimpleNotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        event PropertyChangedSimpleEventHandler PropertyChanged;
    }
}
