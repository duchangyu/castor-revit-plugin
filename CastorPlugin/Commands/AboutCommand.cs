using Autodesk.Revit.Attributes;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Views.Pages;
using Nice3point.Revit.Toolkit.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastorPlugin.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]   
    public class AboutCommand : ExternalCommand
    {
        public override void Execute()
        {

            Host.GetService<ICastorService>()
                .Show<AboutView>();
               
            
        }
    }
}
