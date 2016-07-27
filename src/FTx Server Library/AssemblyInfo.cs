/*
 * $Header: C:/CVS\040Repositories/ESS/ESS/FTx\040Server\040Library/AssemblyInfo.cs,v 1.5 2005/01/24 14:36:59 Todd A. Mancini Exp $
 * @(#)$Id: AssemblyInfo.cs,v 1.5 2005/01/24 14:36:59 Todd A. Mancini Exp $
 *
 * Copyright 2002-2004 by Daxat, Inc.,
 * 35 Hidden Valley Road, Groton, Massachusetts, U.S.A.
 * All rights reserved.
 *
 * This software is the confidential and proprietary information
 * of Daxat, Inc. ("Confidential Information").  You
 * shall not disclose such Confidential Information and shall use
 * it only in accordance with the terms of the license agreement
 * you entered into with Daxat.
 */
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle("Daxat.Ess.ServerLibrary")]
[assembly: AssemblyDescription("Server-side class library for interacting with the Daxat Extensible Search Server.")]
#if DEBUG
[assembly:AssemblyConfiguration("Debug Build")]
#else
[assembly:AssemblyConfiguration("Release Build")]
#endif
[assembly: AssemblyCompany("Daxat, Inc.")]
[assembly: AssemblyProduct("Ess")]
[assembly: AssemblyCopyright("Copyright � 2002-2004 Daxat, Inc., All rights reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]		

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion("1.0.0.0")]

//
// In order to sign your assembly you must specify a key to use. Refer to the 
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing. 
//
// Notes: 
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. KeyFile refers to a file which contains
//       a key.
//   (*) If the KeyFile and the KeyName values are both specified, the 
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP, that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key 
//           in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//       When specifying the KeyFile, the location of the KeyFile should be
//       relative to the project output directory which is
//       %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//       located in the project directory, you would specify the AssemblyKeyFile 
//       attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//       documentation for more information on this.
//
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile(@"..\..\FTxServerLibrary.snk")]
[assembly: AllowPartiallyTrustedCallers()]
//[assembly: AssemblyKeyFile("")]
[assembly: AssemblyKeyName("")]

[assembly: CLSCompliant(true)]