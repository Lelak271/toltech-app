using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toltech.App.Views;

// NameResolvable.cs
namespace Toltech.App.Models
{
    public interface INameResolvable
    {
        string Name { get; set; }
    }

    public partial class Requirements : INameResolvable
    {
        string INameResolvable.Name
        {
            get => NameReq;
            set => NameReq = value;
        }
    }

    public partial class Part : INameResolvable
    {
        string INameResolvable.Name
        {
            get => NamePart;
            set => NamePart = value;
        }
    }

    public partial class ModelData : INameResolvable
    {
        string INameResolvable.Name
        {
            get => Model;
            set => Model = value;
        }
    }
}
