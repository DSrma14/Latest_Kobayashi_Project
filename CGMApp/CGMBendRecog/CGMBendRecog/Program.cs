using System;
using System.Diagnostics;
using Spatial.CGM;
using IopCgmSample;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;



namespace CGMBendRecog
{
  class Program
  {
    static int Main(string[] args)
    {
      // MessageBox.Show("message", "caption", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
      int result = 0;
      CGMBendRecognitionSample sample = new CGMBendRecognitionSample();

      try
      {
        //string CGMPATH = @"E:\Spatial\CGM_2021.1.0.1_TAMpkg_04072021\win_b64\code\bin";
        string CGMPATH = Environment.GetEnvironmentVariable("CGMPATH");
        string DLLs = $"{CGMPATH}\\win_b64\\code\\clr;{CGMPATH}\\win_b64\\code\\bin";
#if DEBUG
        Console.WriteLine($"CGMPATH read from Env: {CGMPATH}");
#endif

        string PATH = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", DLLs + ";" + PATH);

        // configure input arguments
        string[] permissibleArgs = { "-i", "-o", "-h", "-s" };

        ArgParser1 parser = new ArgParser1(args, permissibleArgs);


        if (parser.isSuccess())
        {

          string value = "";
          if (parser.GetArgValue("-h", out value))
            CGMBendRecognitionSample.PrintHelp();
          else
          {
            string inputFilePath = "";
            string outputFilePath = "";
            string pTargetFilePath = null;


            bool hasInputFileName = parser.GetArgValue("-i", out inputFilePath);
            bool hasOutputFileName = parser.GetArgValue("-o", out outputFilePath);
            bool hasStepFile = parser.GetArgValue("-s", out pTargetFilePath);

            //bool hasOutputFileName = parser.GetArgValue("-u", out outputFilePath);

            if (hasInputFileName)
            {
              if (hasOutputFileName == false)
              {
                // output JSON to a default location 
                outputFilePath = Environment.GetEnvironmentVariable("TEMP") + @"\CGMBendRecogOutput.json";
              }
              Console.WriteLine("Input: " + inputFilePath);
              Console.WriteLine("Output: " + outputFilePath);

              // Initialize CGM
              SPAIopCGMSystem.Initialize(SPATIAL_LICENSE.KEY);
              Console.WriteLine("CGM initialized successfully!");


             
              //sample.LoadXCGMPart("Enclosure-Inner-part.XCGM", ref container);
              bool sampleResult = sample.RecognizeBendsToJSON(inputFilePath, outputFilePath);
              
              Console.WriteLine("sample result returns: " + sampleResult.ToString());
              // to unbend
              if(hasStepFile)
              {
                IntPtr cPtr=new IntPtr();
                bool cMemoryOw=new bool();
                CGMContainer ioCgmContainer=new CGMContainer(cPtr,cMemoryOw); ;
                sample.LoadXCGMPart(inputFilePath, ref ioCgmContainer);
                CGMPart part = ioCgmContainer.GetPart();
                CATBody iBody= null;
                CATListPtrCATBody oBodyList=new CATListPtrCATBody();
                CGMModificationResult res = new CGMModificationResult();
                part.GetBodies(oBodyList);
                List<CATBody> listofbodies = new List<CATBody>();
                {
                  foreach (CATBody b in oBodyList)
                  {
                    if (sample.HowToQueryForSolidBody(b))
                    {
                      listofbodies.Add(b);
                      Console.WriteLine($"Detect Solid Body - Persistent Tag = {b.GetPersistentTag()}");
                    }
                  }
                  iBody = listofbodies[0];
                }
                // unbend 
               CGMModificationResult r1 = new CGMModificationResult();
               r1= sample.HowToAutoUnbendSheetMetals(part, iBody);
                if (r1.IsValid())
                {
                  Console.WriteLine("Unbending processing ");
                  bool exportResult = new bool(); ;
                  exportResult = sample.ExportToStep(pTargetFilePath, ioCgmContainer);
                  if(exportResult)
                  {
                    Console.WriteLine("Successfully exported in Step");
                  }
                }                  
              }

            }
            else
            {
              CGMBendRecognitionSample.PrintHelp();
              Console.WriteLine("Error Message: No input file path");
              result = 1;
            }
          }
        }
        else
        {
          CGMBendRecognitionSample.PrintHelp();
          Console.Write("\n Error Message = Invalid arguments\n");
          result = 1;
        }
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(string.Format("ERROR: " + e.Message));
        result = 1;
      }
      return result;
    }

    class CGMBendRecognitionSample
    {
      string _inputPartFile;
      CGMContainer _container;
      List<CATFeatureInfoBend> _bendFeatures;
      //List<CATFeatureInfoFillet> _filletFeatures;

      public CGMBendRecognitionSample()
      {
        _inputPartFile = "dummy_name";
        _container = null;
        _bendFeatures = new List<CATFeatureInfoBend>();
        //_filletFeatures = new List<CATFeatureInfoFillet>();
      }

      public bool RecognizeBendsToJSON(in string iSourceFilePath, in string iTargetFilePath)
      {
        bool result = true;
        try
        {
          result = LoadXCGMPart(iSourceFilePath, ref _container);
          if (result)
            Console.WriteLine("Part is loaded Successfully");
          else
            return false;

          _inputPartFile = iSourceFilePath;

          CGMPart part = _container.GetPart();
          Debug.Assert(part != null);

          CATBody body = null;
          List<CATBody> listofbodies = new List<CATBody>();
          {
            CATListPtrCATBody bodies = new CATListPtrCATBody();
            part.GetBodies(bodies);
            if (bodies.Size() == 0)
              throw new Exception("File does not contain any bodies");

            foreach (CATBody b in bodies)
            {
              if (HowToQueryForSolidBody(b))
              {
                listofbodies.Add(b);
                Console.WriteLine($"Detect Solid Body - Persistent Tag = {b.GetPersistentTag()}");
              }
            }
          }
          if (listofbodies.Count > 1)
            throw new Exception("File contains more than one solid body");
          if (listofbodies.Count == 0)
            throw new Exception("File does not contain a solid body");

          body = listofbodies[0];


          // start recognize bend information
          _bendFeatures.Clear(); // remove all elements if there are any
          HowToGetBendsFromBody(body);
          CGMModificationResult res = new CGMModificationResult();
          //if (_bendFeatures.Count > 0)
          //{
          //  HowToAutoUnbendSheetMetals(part, body, res);
          // // sample.ExportToStep(pTargetFilePath);

          //}
          //else
          //{
          //  Console.WriteLine("Not contain any bend");
          //}

          //if (_bendFeatures.Count < 1 && _filletFeatures.Count < 1)
          //{
          //    Console.WriteLine("No bend or fillet is detected. Exiting ...");
          //    return false;
          //}

          // write bend info to JSON
          result = ExportToJSON(iTargetFilePath);
          if (result)
            Console.WriteLine("Successfully write JSON to disk.");
            

        }
        catch
        {
          Console.WriteLine("RecognizeBendsToJSON failed!");
          throw;
        }

        return result;
      }

      public bool ExportToJSON(in string iTargetFilePath)
      {
        bool result = true;
        try
        {
          int totalBends = _bendFeatures.Count;

         

          //int totalFillets = _filletFeatures.Count;
          // Streaming
          using (StreamWriter sw = new StreamWriter(iTargetFilePath))
          {
            sw.WriteLine("{\n\"partFile\" : \"" + _inputPartFile.Replace(@"\", @"\\") + "\",");

            int counter = 1;

            sw.WriteLine();

            double minRadius = -1, maxRadius = -1, angle = -1; // in radian
            CATListPtrCATFace side_1 = new CATListPtrCATFace(), side_2 = new CATListPtrCATFace();
            List<uint> tags_1 = new List<uint>(), tags_2 = new List<uint>();

            //sw.WriteLine($"\"numBends\": {_bendFeatures.Count}, \"bendInfo\" : [ ");
            sw.WriteLine($"\"bends\" : [ ");

            // construct strings
            foreach (CATFeatureInfoBend bendInfo in _bendFeatures)
            {
              bendInfo.GetBendParameters(ref minRadius, ref maxRadius, ref angle);


              Console.Write("Min Radius=");
              Console.Write(minRadius);
              Console.WriteLine("max Radius=");
              Console.Write(maxRadius);

              bendInfo.GetFaces(side_1, side_2);
              foreach (CATFace f in side_1)
              {
                tags_1.Add(f.GetPersistentTag());
              }
              foreach (CATFace f in side_2)
              {
                tags_2.Add(f.GetPersistentTag());
              }
              
              double thickness = (maxRadius - minRadius);
              string oneBendInfo = string.Format("{{\"faces\": {{\"side_1\": [{0}], \"side_2\":[{1}] }},\"minradius\" : {2:0.##},\"maxradius\" : {3:0.##}, \"thickness\" : {4:0.##}, \"angle\" : {5:0.##}}}",
                  string.Join(",", tags_1), string.Join(",", tags_2), minRadius,maxRadius, thickness, angle); // precision to the 2nd decimal

              if (counter < totalBends)
                oneBendInfo += ",";
              // info for one bend
              sw.WriteLine(oneBendInfo);

              // clean up arrays and variables
              side_1.RemoveAll(); side_2.RemoveAll();
              tags_1.Clear(); tags_2.Clear();
              minRadius = -1; maxRadius = -1; angle = -1;

              counter = counter + 1;
            }
            // ending bendInfo array
            sw.WriteLine(@"]");

            //counter = 1;
            //if (totalFillets > 0)
            //{
            //    CATListPtrCATFace faces = new CATListPtrCATFace();
            //    double radius = -1;
            //    List<uint> fTags = new List<uint>();
            //    sw.WriteLine();

            //    sw.WriteLine($"\"numFillets\": {_filletFeatures.Count}, \"filletInfo\" : [ ");
            //    foreach (CATFeatureInfoFillet filletInfo in _filletFeatures)
            //    {

            //        filletInfo.GetFaces(faces);
            //        foreach (CATFace f in faces)
            //        {
            //            fTags.Add(f.GetPersistentTag());
            //        }

            //        string oneFilletInfo = string.Format("{{\"faces\" : [{0:n2}]", string.Join(",", fTags));
            //        if(filletInfo is CATFeatureInfoFilletConstantRadius)
            //        {
            //            CATFeatureInfoFilletConstantRadius constFillet = filletInfo as CATFeatureInfoFilletConstantRadius;
            //            radius = constFillet.GetFilletRadius();
            //            oneFilletInfo += string.Format(", \"radius\" : {0}}}",radius);
            //        }

            //        if (counter < totalFillets)
            //            oneFilletInfo += ",";

            //        //write info for one fillet to file
            //        sw.WriteLine(oneFilletInfo);

            //        //clean up
            //        faces.RemoveAll(); fTags.Clear();
            //        radius = -1;
            //        counter++;
            //    }
            //    // ending filletInfo array
            //    sw.WriteLine(@"]");

            //}
            sw.WriteLine(@"}");

          }
        }
        catch (Exception e)
        {
          throw new Exception("Failed to write a JSON file - " + e.Message);
        }

        return result;
      }

      /// <summary>
      /// Use 3D InterOp to import a XCGM part file. Assemblies are not supported at this moment. 
      /// </summary>
      /// <param name="iSourceFilePath"></param>
      /// <param name="ioCgmContainer"></param>
      /// <returns>true if the part is imported successfully; false, otherwise</returns>
      public bool LoadXCGMPart(string iSourceFilePath, ref CGMContainer ioCgmContainer)
      {
        bool result = true;



        // Create Source Document from input file
        SPAIopDocument sourceDoc;
        {
          SPAIopWString wiSourceFilePath = new SPAIopWString(iSourceFilePath);
          sourceDoc = new SPAIopDocument(wiSourceFilePath);
        }

        SPAIopInputProductStructure inputPS = new SPAIopInputProductStructure();
        SPAIopProductStructureImporter psImporter = new SPAIopProductStructureImporter();
        try
        {
          SPAIopPSImportResult psResult = psImporter.Import(sourceDoc, ref inputPS);
          if (inputPS.GetRootInstancesCount() != 1)
          {
            Console.WriteLine("Assembly file is not supported. Exiting... ");
            return false;
          }

          SPAIopInputPSInstance inputPartInstance = inputPS.GetRootInstance(0);
          SPAIopInputPSReference inputPartRef = inputPartInstance.GetReference();
          SPAIopPartDocument inputReferenceDoc = inputPartRef.GetDocument();

          SPAIopCGMContainerImporter partImporter = new SPAIopCGMContainerImporter();
          {
            SPAIopCGMContainerImportResult importRes = partImporter.Import(inputReferenceDoc, out ioCgmContainer);
            if (ioCgmContainer == null)
            {
              Console.WriteLine("Null Container after importing a part. Exiting... ");
              return false;
            }
          }

          //                    // Alternative: CGM Unstreaming without InterOp ?  Andrei for Staubli 
          //                    using (CGMContainerUnstream containerUnstreamer = new CGMContainerUnstream())
          //                    {

          //                        CATUnicodeString inputCATStr = new CATUnicodeString(iSourceFilePath);
          //                        CGMContainerLoadResult loadResult = containerUnstreamer.Unstream(inputCATStr, ref ioCgmContainer);
          //#if DEBUG
          //                        Debug.Assert(ioCgmContainer != null, "Loaded CGMContainer is NULL");
          //                        CATUnicodeString verName = new CATUnicodeString();
          //                        loadResult.GetFileVersion().GetName(verName);
          //                        Console.WriteLine("Loaded File Version" + verName.ToString());
          //#endif
          //                    }
        }
        catch (Exception e)
        {
          result = false;
          Console.WriteLine(e);
        }
        return result;
      }

      public void HowToGetBendsFromBody(in CATBody iBody)
      {
        _ = _container ?? throw new ArgumentNullException($"Error at {nameof(HowToGetBendsFromBody)}: property {nameof(CGMContainer)} is null");
        CGMPart iPart = _container.GetPart();
        CATGeoFactory factory = iPart.GetFactory();
        CATSoftwareConfiguration swConfig = iPart.GetSoftwareConfiguration();
        CATFeatureTypes featureTypes = new CATFeatureTypes();
        CATFeatureTypeWallAndBend wallBendType = new CATFeatureTypeWallAndBend(factory, swConfig);
        //featureTypes.Add(wallBendType); // uncomment for non-debug mode

        featureTypes.Add(new CATFeatureTypeWallAndBend());
        //featureTypes.Add(new CATFeatureTypeFillet());
        //featureTypes.Add(new CATFeatureTypeHole());
        //Console.WriteLine($"Detecting {nameof(CATFeatureTypeFillet)} and {nameof(CATFeatureTypeWallAndBend)} ...");
        Console.WriteLine($"Detecting {nameof(CATFeatureTypeWallAndBend)} ...");

        CATFeatureBrowser wbBrowser;
        HowToRunFeatureRecognition(iPart, iBody, featureTypes, out wbBrowser);

        CATFeatureIterator featureIterator = wbBrowser.GetIterator();
        featureIterator.Reset();
        Console.WriteLine($"Total feature(s) recognized: {featureIterator.Size()}");
        while (featureIterator.Next())
        {
          CATFeature feature = featureIterator.GetCurrentFeature();
          CATFeatureInfo featureInfo = feature.GetFeatureInfo();

          //Console.WriteLine(featureInfo.GetType().ToString());
          //Console.WriteLine(wallBendType.GetType().ToString());

          //if(featureInfo is CATFeatureInfoFillet)
          //{
          //    _filletFeatures.Add(featureInfo as CATFeatureInfoFillet);
          //}

          if (featureInfo is CATFeatureInfoBend)
          {
            // populate the list
            _bendFeatures.Add(featureInfo as CATFeatureInfoBend);
          }// else: Wall feature type
#if DEBUG
          //GetFeatureInfoType(featureInfo);
          if (featureInfo is CATFeatureInfoWall)
            Console.WriteLine(" ---- Detect Wall Feature");
#endif
        }
        _bendFeatures.TrimExcess();
        //_filletFeatures.TrimExcess();                        

        //Console.WriteLine($"There are {_bendFeatures.Count} bends and {_filletFeatures.Count} fillets detected");
        Console.WriteLine($"There are {_bendFeatures.Count} bends detected");
      }

      public void GetFeatureInfoType(in CATFeatureInfo iFeatureInfo)
      {
        if (iFeatureInfo is CATFeatureInfoBend)
          Console.WriteLine("-- Bend ");
        if (iFeatureInfo is CATFeatureInfoChamfer)
          Console.WriteLine("-- Chamfer ");
        if (iFeatureInfo is CATFeatureInfoWall)
          Console.WriteLine("-- Wall ");
        if (iFeatureInfo is CATFeatureInfoFillet)
          Console.WriteLine("-- Fillet ");
        if (iFeatureInfo is CATFeatureInfoCutOut)
          Console.WriteLine("-- CutOut ");
        if (iFeatureInfo is CATFeatureInfoHole)
          Console.WriteLine("-- Hole ");
        if (iFeatureInfo is CATFeatureInfoLogo)
          Console.WriteLine("-- Logo ");
        if (iFeatureInfo is CATFeatureInfoPocket)
          Console.WriteLine("-- Pocket ");
        if (iFeatureInfo is CATFeatureInfoPad)
          Console.WriteLine("-- Pad ");
        if (iFeatureInfo is CATFeatureInfoNotch)
          Console.WriteLine("-- Notch ");
        if (iFeatureInfo is CATFeatureInfoSlot)
          Console.WriteLine("-- Slot ");
      }
      /// <summary>
      /// CGM-HowTo:Run Feature Recognition
      /// </summary>
      /// <param name="iPart"></param>
      /// <param name="iBody"></param>
      /// <param name="iFeatureTypes"></param>
      /// <param name="oFeatureBrowser"></param>
      static public void HowToRunFeatureRecognition(in CGMPart iPart, in CATBody iBody, in CATFeatureTypes iFeatureTypes, out CATFeatureBrowser oFeatureBrowser)
      {
        CATGeoFactory factory = iPart.GetFactory();
        CATTopData topData = iPart.GetTopData();

        using (CATICGMRecognizeFeatures recognizeFeatureOp = Globals.CATCGMCreateRecognizeFeatures(factory, topData, iBody, iFeatureTypes))
        {
          iPart.QueryUsing(recognizeFeatureOp);
          oFeatureBrowser = recognizeFeatureOp.GetCATFeatureBrowser();
        }
      }

      public CGMModificationResult HowToAutoUnbendSheetMetals(CGMPart ipart, CATBody iBody)
      {
        CGMModificationResult res = new CGMModificationResult();
        CATGeoFactory ifactory = ipart.GetFactory();
        CATTopData topdata = ipart.GetTopData();
        double i_kfactor = 0.33;
        CATListPtrCATCell oBodyFaces = new CATListPtrCATCell();
        getBodyFaces(iBody, oBodyFaces);
        CATListPtrCATFace listface = new CATListPtrCATFace();
        GetTopandBottomFace(ipart, oBodyFaces, listface);
        CATFace ipInvariantFace = listface[1];
        using (CATICGMTopUnbendSheetMetal op = Globals.CATCGMCreateTopUnbendSheetMetal(ifactory, topdata, iBody, ipInvariantFace, i_kfactor))
        {
          res = ipart.ModifyUsing(op);

        }
        return res;

      }

    public double HowToCalculateAreaOfFace(CGMPart iPart, CATFace ipFace)
    {
      CATTopData pTopData = iPart.GetTopData();
      using (CATICGMDynMassProperties3D massProperties3DOp = Globals.CATCGMDynCreateMassProperties3D(pTopData, ipFace))
      {
        iPart.QueryUsing(massProperties3DOp);
        return massProperties3DOp.GetWetArea();
      }

    }


    public void GetTopandBottomFace(CGMPart pPart, CATListPtrCATCell BodyCells, CATListPtrCATFace listface)
    {
      // part -->body -->face
      Dictionary<CATFace, double> DFace = new Dictionary<CATFace, double>(); ;
      double area = 0;
      double maxarea = 0.00;
      for (int ii = 1; ii <= BodyCells.Size(); ii++)
      {
        area = HowToCalculateAreaOfFace(pPart, (CATFace)BodyCells[ii]);
        DFace.Add((CATFace)BodyCells[ii], area);
      }
      List<KeyValuePair<CATFace, double>> lis = new List<KeyValuePair<CATFace, double>>();
      foreach (var item in DFace)
      {
        KeyValuePair<CATFace, double> k1 = new KeyValuePair<CATFace, double>(item.Key, item.Value);
        lis.Add(k1);
      }
      lis.Sort(delegate (KeyValuePair<CATFace, double> t1, KeyValuePair<CATFace, double> t2)
      { return (t1.Value.CompareTo(t2.Value)); });

      listface.RemoveAll();

      if (listface.Size() != 1)
      {
        listface.Append(lis[lis.Count - 1].Key);
      }
      DFace.Clear();
      lis.Clear();
    }

    public void getBodyFaces(CATBody pCgmBody, CATListPtrCATCell oBodyFaces1)
    {
      CATListPtrCATCell listCells = new CATListPtrCATCell();
      pCgmBody.GetAllCells(listCells, 2);
      // Vector<CATFace> vectFeatureFaces=new Vector<CATFace>();
      List<CATFace> listFeatureFaces = new List<CATFace>();
      foreach (var item in _bendFeatures)
      {
        CATListPtrCATFace listFace = new CATListPtrCATFace();
        item.GetFaces(listFace);
        foreach (var item1 in listFace)
        {

          listFeatureFaces.Add((CATFace)item1);
        }
      }

      for (int ii = 1; ii <= listCells.Size(); ii++)
      {
        if (listCells[ii] != null)
        {
          oBodyFaces1.Append(listCells[ii]);
        }
      }
    }

      //export to STEP and STP

    public bool ExportToStep(string oSourceFilePat,CGMContainer container)
    {
        bool result = true;
      SPAIopWString wiSourceFilePath = new SPAIopWString(oSourceFilePat);
      SPAIopOutputProductStructure outputPS = new SPAIopOutputProductStructure();
      SPAIopCGMOutputPart partData = new SPAIopCGMOutputPart();
      bool returnValue = true;
      SPAIopOutputPSReference iOutRef = new SPAIopOutputPSReference(outputPS, 42);
      partData.SetContainer(container);
      iOutRef.SetOutputPartData(partData);
      SPAIopPartExporter exporter = new SPAIopPartExporter();
      SPAIopPolicy policy = new SPAIopPolicy();
      try
      {
        SPAIopResult res = exporter.ExportWithPolicy(policy, partData, wiSourceFilePath);
      }
      catch (Exception fatalErr)
      {
        Console.WriteLine(fatalErr);
        returnValue = false;
      }
        return result;
    }

      int checkUniformThickness(CGMPart pPart, CATListPtrCATCell BodyCells, CATBody pCgmBody)
      {
        int res = 0;
        IntPtr cPtr = new IntPtr(); ;
        bool cMemoryOwn=new bool();
        CATListPtrCATFace listface = new CATListPtrCATFace();
        if (BodyCells.Size() > 0)
          GetTopandBottomFace(pPart, BodyCells, listface);
        if (listface.Size() == 2)
        {
          CATFace topface = listface[1];
          CATFace bottomface = listface[2];
          CATSurface topSurf = topface.GetSurface();
          CATSurface bottomSurf = bottomface.GetSurface();
          CATSurLimits ptbottomlim = bottomSurf.GetLimits();

          CATListPtrCATCell elist=new CATListPtrCATCell();
          CATMathVector oTopFaceNormal=new CATMathVector(0, 0, 0);
          topface.GetAllCells(elist,0);
          HowToQueryForNormalOfFaceAtVertex(pPart, pCgmBody, topface, (CATVertex)elist[1], oTopFaceNormal);
          elist.RemoveAll();

          CATTopData pTopData = pPart.GetTopData();
          CATMathPoint oCenter;
          using (CATICGMDynMassProperties3D massProperties3DOp = Globals.CATCGMDynCreateMassProperties3D(pTopData, topface))
          {
            pPart.QueryUsing(massProperties3DOp);
             oCenter = massProperties3DOp.GetCenterOfGravity();
          }

          CATMathVector oBottomFaceNormal = new CATMathVector(0, 0, 0);
          bottomface.GetAllCells(elist, 0);
          HowToQueryForNormalOfFaceAtVertex(pPart, pCgmBody, bottomface, (CATVertex)elist[1], oBottomFaceNormal);
          

          
          double angle= HowToGetAngleBetweenVectors(oTopFaceNormal, oBottomFaceNormal);
          double degree = angle * (180 / Globals.CATPI);
          degree = Math.Ceiling(degree);
          if (degree == 0 || degree == 180)
          {
            Console.WriteLine("Modal is planar");
            res = 1;
          }
          else
          {
            CGMBendRecognitionSample.PrintHelp();
            Console.Write("\nFailure -Non uniform thickness found\n");
          }
        }
          return res;
      }

      void HowToGetBoundedCells(CATBody ipBody, CATListPtrCATCell iCells, CATListPtrCATCell oBoundedCells)
      {
        List<CATCell> boundedCells = new List<CATCell>();
        for (int i = 1; i <= iCells.Size(); i++)
        {
          CATCell cell = iCells[i];
          if (cell==null) continue;
          CATBoundedCellsIterator bcItr = cell.CreateBoundedCellsIterator(ipBody);
          if (bcItr == null)
          {
            continue;
          }
          IntPtr cPtr = new IntPtr(); ;
          bool cMemoryOwn=new bool();
          CATCell boundedCel = new CATCell(cPtr,cMemoryOwn);
          while (bcItr.Next()!=null)
          {
            boundedCells.Add(bcItr.Next());
          }
        }
        foreach (var bc in boundedCells)
        {
          oBoundedCells.Append(bc);
        }

      }
      void HowToGetEdgesAtVertex(CATVertex ipVert,CATBody ipBody,CATListPtrCATEdge oEdgeList)
      {
        CATListPtrCATCell cells = new CATListPtrCATCell();
        CATListPtrCATCell bounded=new CATListPtrCATCell();
        cells.Append(ipVert);
        HowToGetBoundedCells(ipBody, cells, bounded);
        int nbBounded = bounded.Size();
        for (int ii = 1; ii <= nbBounded; ++ii)
        {
          CATCell b = bounded[ii];
          if (b is CATEdge)
          {
            oEdgeList.Append((CATEdge)b);
          }
        }
      }

      void HowToQueryForNormalOfFaceAtVertex(CGMPart iPart, CATBody ipBody, CATFace ipFace, CATVertex ipVertex, CATMathVector oNormal)
      {
        CATSurLimits surLimits = new CATSurLimits();
        surLimits = ipFace.Get2DBoundingBox();
        double uhigh = new double();
        double ulow = new double();
        double vhigh = new double();
        double vlow = new double();
        surLimits.GetExtremities(ref ulow, ref vlow, ref uhigh, ref vhigh);
        CATListPtrCATEdge edges = new CATListPtrCATEdge();
        HowToGetEdgesAtVertex(ipVertex, ipBody, edges);
        if (edges.Size() == 0)
        {
          throw new Exception("Input vertex must bound at least one edge");
        }
        IntPtr cPtr = new IntPtr();
        bool cMemoryOwn = new bool();
        CATEdge vertexEdge = new CATEdge(cPtr, cMemoryOwn);
        short verSide = new short();
        int i = 0;
        for (i = 1; i <= edges.Size(); i++)
        {
          int count = 0;
          CATBoundedCellsIterator boundedCellsIterator = edges[i].CreateBoundedCellsIterator(ipBody);
          if (null == boundedCellsIterator)
          {
            throw new Exception("The iterator could not be created.");
          }
          short vertexEdgeSide = new short();
          vertexEdgeSide = (short)Globals.CATSideUnknown;
          while (boundedCellsIterator.Next(ref vertexEdgeSide) != null)
          {
            if (ipFace == boundedCellsIterator.Next(ref vertexEdgeSide))
            {
              count++;
              vertexEdge = edges[i];
            }
          }
          if (count == 1)
          {
            break;
          }
        }

        if (null == vertexEdge)
        {
          throw new Exception("Input vertex does not lie in the given face");
        }
        CATPointOnEdgeCurve pointOnEdgeCurve =ipVertex.GetGeometryOnEdge(vertexEdge);
        CATPCurve edgePCurve = vertexEdge.GetGeometryOnFace(ipFace, verSide);
        CATCrvParam vertexParamOnEdge=new CATCrvParam();
        if (null != pointOnEdgeCurve)
        {
          vertexParamOnEdge = pointOnEdgeCurve.GetParamOn(edgePCurve);
        }
        else
        {
          throw new Exception("Input vertex has no geometry");
        }
        CATSurParam vertexParamOnFace=new CATSurParam();
        if (null != edgePCurve)
        {
          vertexParamOnFace = edgePCurve.EvalPointUV(vertexParamOnEdge);
        }
        else
        {
          throw new Exception(
            "Could not find the surface parameter for the input vertex");
        }
        ipFace.EvalNormal(ref vertexParamOnFace, ref oNormal);
        // Get the shell this face lives in
        CATListPtrCATDomain shells = new CATListPtrCATDomain(); ;
        ipBody.GetAllDomains(2, 3, shells);
        int j = 0;
        short orientation = new short();
        orientation = (short)Globals.CATOrientationUnknown;
        for (j = 1; j <= shells.Size(); j++)
        {
          if (shells[j] != null)
          {
            shells[j].Owns(ipFace, ref orientation);
            break;
          }     
        }
        if (Globals.CATOrientationUnknown == orientation)
        {
          throw new Exception("Could not find the shell of the input face");
        }
        oNormal *= orientation;
      }

     double HowToGetAngleBetweenVectors(CATMathVector iVector1, CATMathVector iVector2)
      {
        double angle = 0.0;
        if (iVector1.SquareNorm() != 0.0 && iVector2.SquareNorm() != 0.0)
        {
          angle = iVector1.GetAngleTo(iVector2);
        }
        else
        {
          throw new Exception ("At least one of the vectors has zero length.");
        }
        return angle;
      }

      double HowToCalculateDistanceBeweenSurfaces(CGMPart iPart,CATSurface ipSurface1,CATSurface ipSurface2,CATSurLimits  iSurfaceLimits1 ,CATSurLimits iSurfaceLimits2)
      {
        CGMPartRollback iPartRollBack=new CGMPartRollback(iPart);
        CGMModificationResult result1 = HowToCreateSheetFromSurface(iPart, ipSurface1, iSurfaceLimits1);
        CGMModificationResult result2 = HowToCreateSheetFromSurface(iPart, ipSurface2, iSurfaceLimits2);


        // Retrieve the two sheet bodies we created.
        CATBody pBody1 = result1.GetResultBody();
        CATBody pBody2 = result2.GetResultBody();

        //return HowToCalculateDistanceBetweenBodies(iPart, pBody1, pBody2);
        return 1;
      }

   class CGMPartRollback
      {
    
        IntPtr cPtr = new IntPtr();
        bool cMemory = new bool();
        CGMPart pPart; 
        CGMStateId initialStateId;
        public CGMPartRollback(CGMPart ipart)
        {
          pPart = new CGMPart(cPtr, cMemory);
          pPart = ipart;
          initialStateId = new CGMStateId(pPart.NoteState());
        }
      }

      CGMModificationResult HowToCreateSheetFromSurface(CGMPart  iPart,CATSurface ipSurface, CATSurLimits iSurLimits)
      {
        CATGeoFactory pGeoFactory = iPart.GetFactory();
        CATTopData pTopData = iPart.GetTopData();
        CATICGMTopSkin skinOp = Globals.CATCGMCreateTopSkin(pGeoFactory, pTopData, ipSurface, iSurLimits);
        return iPart.ModifyUsing(skinOp);
      }


      /// <summary>
      /// Reference C++ HowTo: https://doc.spatial.com/get_doc_page/articles/d/e/t/CGM-HowTo~Determine_if_a_Body_Is_a_Point,_Wire,_Sheet,_Solid,_or_Mixed_Body_a2ab.html#Headers_4
      /// </summary>
      /// <param name="iBody"></param>
      /// <returns>true if the body is solid; false, otherwise </returns>
       public bool HowToQueryForSolidBody(in CATBody iBody)
      {
        _ = iBody ?? throw new ArgumentNullException($"Error at {nameof(HowToQueryForSolidBody)}: input {nameof(CATBody)} is null");

        bool answer = false;
        int cellsMaxDim = -1;
        bool isHomogenuous = true;
        iBody.GetCellsHighestDimension(ref cellsMaxDim, ref isHomogenuous);
        answer = (cellsMaxDim == 3) && isHomogenuous;
        return answer;
      }

      static public void PrintHelp()
      {
        Console.Write("Usage: CGMBendRecog -i <full-input-file-path> -o <full-output-file-path> [-h] \n");
        Console.Write("\t-i  : The absolute path to the input file. \n");
        Console.Write("\t-o  : The absolute path to the output file (.json). \n");
        Console.Write("\t-h  : This help.\n");
      }

    }

  }


}
