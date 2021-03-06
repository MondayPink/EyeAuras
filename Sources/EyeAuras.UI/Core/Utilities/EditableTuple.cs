﻿using ReactiveUI;

namespace EyeAuras.UI.Core.Utilities
{
    internal sealed class EditableTuple<T1, T2> : ReactiveObject
    {
        private T1 item1;

        private T2 item2;

        public T1 Item1
        {
            get => item1;
            set => this.RaiseAndSetIfChanged(ref item1, value);
        }

        public T2 Item2
        {
            get => item2;
            set => this.RaiseAndSetIfChanged(ref item2, value);
        }
    }
}