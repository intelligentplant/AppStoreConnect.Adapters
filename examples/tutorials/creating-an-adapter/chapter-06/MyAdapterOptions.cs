using DataCore.Adapter;

namespace MyAdapter {
    public class MyAdapterOptions : AdapterOptions {

        public double Period { get; set; } = 60;

        public double Amplitude { get; set; } = 1;

    }
}
