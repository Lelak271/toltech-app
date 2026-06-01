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
        int Id { get; }
    }

    public partial class Requirements : INameResolvable
    {
        string INameResolvable.Name
        {
            get => NameReq;
            set => NameReq = value;
        }
        int INameResolvable.Id => Id_req;
    }

    public partial class Part : INameResolvable
    {
        string INameResolvable.Name
        {
            get => NamePart;
            set => NamePart = value;
        }
        int INameResolvable.Id => Id;
    }

    public partial class ModelData : INameResolvable
    {
        string INameResolvable.Name
        {
            get => Model;
            set => Model = value;
        }
        int INameResolvable.Id => Id;
    }
}
