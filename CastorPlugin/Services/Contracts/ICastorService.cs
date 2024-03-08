// Copyright 2003-2023 by Autodesk, Inc.
// 
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
// 
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
// 
// Use, duplication, or disclosure by the U.S. Government is subject to
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.

using System.Windows.Controls;

namespace CastorPlugin.Services.Contracts;

public interface ICastorService : ICastorServiceDependsStage, ICastorServiceShowStage, ICastorServiceExecuteStage
{
    //ICastorServiceDependsStage Snoop(SnoopableType snoopableType);
    //ICastorServiceDependsStage Snoop(SnoopableObject snoopableObject);
    //ICastorServiceDependsStage Snoop(IReadOnlyCollection<SnoopableObject> snoopableObjects);
    new ICastorServiceShowStage DependsOn(IServiceProvider provider);
    new ICastorServiceExecuteStage Show<T>() where T : Page;
}

public interface ICastorServiceDependsStage
{
    ICastorServiceShowStage DependsOn(IServiceProvider provider);
    ICastorServiceExecuteStage Show<T>() where T : Page;
}

public interface ICastorServiceShowStage
{
    ICastorServiceExecuteStage Show<T>() where T : Page;
}

public interface ICastorServiceExecuteStage
{
    void Execute<T>(Action<T> handler) where T : class;
}