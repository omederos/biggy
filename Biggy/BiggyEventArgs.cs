using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy {
  public class BiggyEventArgs<T> : EventArgs {
    public List<T> Items { get; set; }
    public dynamic Item { get; set; }

    public BiggyEventArgs() {
      Items = new List<T>();
      this.Item = default(T);
    }
  }
}
