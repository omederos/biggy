using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy {
  
  public class BiggyDB : DynamicObject {

    BiggyList<dynamic> _existing;
    public override bool TryGetMember(GetMemberBinder binder, out object result) {
      //return base.TryGetMember(binder, out result);
      _existing = _existing ?? new BiggyList<dynamic>(dbName: binder.Name);
      result = _existing;
      return true;
    }
  }
}