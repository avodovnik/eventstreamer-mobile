using System;
namespace SensorCollector
{
    public class SelectableItem<T>
    {
        public T Data { get; set; }
        public bool Selected { get; set; }

        public SelectableItem(T item)
        {
            this.Data = item;
            this.Selected = false;
        }
    }
}
