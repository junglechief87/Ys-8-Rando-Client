using ReactiveUI;
using System;

namespace Ys8AP.ViewModels
{
    public class ViewModelBase : ReactiveObject, IDisposable
    {
        public virtual void Dispose()
        {
        }
    }
}
