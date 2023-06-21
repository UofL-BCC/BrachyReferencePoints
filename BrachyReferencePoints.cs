using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Microsoft.VisualBasic;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
  public class Script
  {
    public Script()
    {
    }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context /*, System.Windows.Window window, ScriptEnvironment environment*/)
        {
            context.Patient.BeginModifications();

            BrachyPlanSetup brachyPlan = context.BrachyPlanSetup;


            Image planImage = brachyPlan.StructureSet.Image;

            var caths = brachyPlan.Catheters;

            double applicatorLength = caths.FirstOrDefault().ApplicatorLength;

            var Shape = caths.First().Shape;

            var SourcePosition = caths.First().SourcePositions;

            int NumberofSourcePositions = SourcePosition.Count();

            SourcePosition LastSourcePosition = caths.First().SourcePositions.Last();

            SourcePosition FirstSourcePosition = caths.First().SourcePositions.First();

            VVector tipCoordinates = new VVector(caths.First().Shape[0].x, caths.First().Shape[0].y, caths.First().Shape[0].z);

            //VVector CenterOfActiveLentgh = LastSourcePosition.Translation - FirstSourcePosition.Translation;

            //sourceposition.translation = coordinates
            //first source position center
            VVector PositiveFirstPosition = new VVector(FirstSourcePosition.Translation.x, FirstSourcePosition.Translation.y, FirstSourcePosition.Translation.z);




            //find the unit vectors from catheter tip to center of first and last source positions
            //print them to compare

            var TipToFirstPosUnitVect = FirstSourcePosition.Translation - Shape[0];
            TipToFirstPosUnitVect.ScaleToUnitLength();

            var TipToLastPosUnitVect = LastSourcePosition.Translation - Shape[0];
            TipToLastPosUnitVect.ScaleToUnitLength();




            //find distance from cath tip to center of first source position
            double DistToCenterFirstSourcePos = caths.First().GetSourcePosCenterDistanceFromTip(FirstSourcePosition);



            //subtract 0.175(half length of source) from that distance to find distance to beginning of source
            //then multiply that to the unit vector to scale the unit vector
            //then add the scaled unit vector to the catheter tip coordinates
            //VVector BeginningCoordOfActiveLength = Shape[0] + (DistToCenterFirstSourcePos - 0.175) * TipToFirstPosUnitVect;
            VVector BeginningCoordOfActiveLength = Shape[0] + (DistToCenterFirstSourcePos) * TipToFirstPosUnitVect;
  

            StringBuilder stringBuilder = new StringBuilder();

  
            //find distance from cath tip to center of last source position
            double DistToCenterLastSourcePos = caths.Last().GetSourcePosCenterDistanceFromTip(LastSourcePosition);

            //active length
            double ActiveLength = caths.First().StepSize * caths.First().SourcePositions.Count();

            //vvector of ending point of active length
            VVector EndingCoordOfActiveLength = BeginningCoordOfActiveLength + (ActiveLength) * TipToLastPosUnitVect;


            var EndToBeginningVect = EndingCoordOfActiveLength - BeginningCoordOfActiveLength;
            double LengthOFVect = EndToBeginningVect.Length;
            EndToBeginningVect.ScaleToUnitLength();

            var HalfWayPoint = BeginningCoordOfActiveLength + (ActiveLength / 2) * TipToLastPosUnitVect;


            VVector PerpendicularVect = GetPerpendicularVector(EndToBeginningVect);



            Structure Cylinder = brachyPlan.StructureSet.Structures.Where(c => c.Id.ToLower().Contains("cyli")).FirstOrDefault();
        

            List<ReferencePoint> appendListRefPointA2 = new List<ReferencePoint>();
            List<ReferencePoint> appendListRefPointA1 = new List<ReferencePoint>();
            List<ReferencePoint> appendListRefPointA3 = new List<ReferencePoint>();

            string DisplayMessage = "";
            string NoFindDisplayMessage = "";


            if (Cylinder !=  null)
            {


                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a2").Any())
                {

                    bool k = true;
                    int i = 1;

                    List<VVector> CheckVectList = new List<VVector>();

                    ReferencePoint A2 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a2").First();
         

                    while (k == true & i < 1000000)
                    {


                        VVector CheckVect = HalfWayPoint + PerpendicularVect * i / 20;

                        bool check = Cylinder.IsPointInsideSegment(CheckVect);

                        k = check;

                        if (check == false)
                        {
                            CheckVectList.Add(CheckVect);

                            i = 900000000;
                        }

                        i++;
                    }




                    if (CheckVectList.Any())
                    {
                        VVector EdgeVect = CheckVectList.First();

                        VVector EdgeVect1 = EdgeVect + PerpendicularVect * 5;

                        A2.ChangeLocation(brachyPlan.StructureSet.Image, EdgeVect1.x, EdgeVect1.y, EdgeVect1.z, stringBuilder);

                        appendListRefPointA2.Add(A2);

                    }



                    DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A2.Id);

                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A2 not found.\n";
                }

                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a1").Any())
                {

                    List<VVector> CheckVectList = new List<VVector>();

                    ReferencePoint A1 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a1").First();



                    int l = 1;
                    bool j = true;
                    while (j == true & l < 1000000)
                    {


                        VVector CheckVect = BeginningCoordOfActiveLength - EndToBeginningVect * l / 20;

                        bool check = Cylinder.IsPointInsideSegment(CheckVect);

                        j = check;

                        if (check == false)
                        {
                            CheckVectList.Add(CheckVect);

                            l = 900000000;
                        }

                        l++;
                    }


                    if (CheckVectList.Any())
                    {
                        VVector EdgeVect = CheckVectList.First();

                        VVector EdgeVect1 = EdgeVect - EndToBeginningVect * 5;

                        A1.ChangeLocation(brachyPlan.StructureSet.Image, EdgeVect1.x, EdgeVect1.y, EdgeVect1.z, stringBuilder);

                        DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A1.Id);

                        appendListRefPointA1.Add(A1);

                    }


                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A1 not found.\n";
                }

                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a3").Any() & brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a1").Any())
                {

                    List<VVector> CheckVectList = new List<VVector>();

                    ReferencePoint A1 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a1").First();

                    ReferencePoint A3 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a3").First();




                    VVector EdgeVect = A1.GetReferencePointLocation(brachyPlan.StructureSet.Image);

                    VVector EdgeVect1 = EdgeVect + PerpendicularVect * 5;

                    A3.ChangeLocation(brachyPlan.StructureSet.Image, EdgeVect1.x, EdgeVect1.y, EdgeVect1.z, stringBuilder);

                    appendListRefPointA3.Add(A3);




                    DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A3.Id);


                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A3 not found.\n";
                }

                

            }
            else
            {
                //cylinder == null


                string CylinderDiameterString = Interaction.InputBox("Would you like to enter a cylinder diameter?", "No Cylinder Contour Found", "Cylinder Diamater in cm");

                double CylinderDiameter = double.Parse(CylinderDiameterString);



                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a2").Any())
                {
                  
                    ReferencePoint A2 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a2").First();



                    VVector EdgeVect1 = HalfWayPoint + PerpendicularVect * CylinderDiameter * 0.5 * 10 + PerpendicularVect*5;


                    A2.ChangeLocation(brachyPlan.StructureSet.Image, EdgeVect1.x, EdgeVect1.y, EdgeVect1.z, stringBuilder);

                    appendListRefPointA2.Add(A2);



                    DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A2.Id);

                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A2 not found.\n";
                }

                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a1").Any())
                {

                    List<VVector> CheckVectList = new List<VVector>();

                    ReferencePoint A1 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a1").First();


                    VVector EdgeVect = BeginningCoordOfActiveLength - EndToBeginningVect * 9;
                    //VVector EdgeVect = BeginningCoordOfActiveLength;



                    A1.ChangeLocation(brachyPlan.StructureSet.Image, EdgeVect.x, EdgeVect.y, EdgeVect.z, stringBuilder);

                    appendListRefPointA1.Add(A1);


                    DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A1.Id);

                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A1 not found.\n";
                }

                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a3").Any() & brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a1").Any())
                {

    

                    ReferencePoint A1 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a1").First();

                    ReferencePoint A3 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a3").First();




                    VVector EdgeVect = A1.GetReferencePointLocation(brachyPlan.StructureSet.Image);

                    VVector EdgeVect1 = EdgeVect + PerpendicularVect * 5;

                    A3.ChangeLocation(brachyPlan.StructureSet.Image, EdgeVect1.x, EdgeVect1.y, EdgeVect1.z, stringBuilder);

                    appendListRefPointA3.Add(A3);


                    DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A3.Id);


                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A3 not found.\n";
                }


            }

    
            ReferencePoint a2 = appendListRefPointA2.FirstOrDefault();
            ReferencePoint a1 = appendListRefPointA1.FirstOrDefault();
            ReferencePoint a3 = appendListRefPointA3.FirstOrDefault();

            if (a2 != null)
            {
                VVector a2Coords = a2.GetReferencePointLocation(brachyPlan);
                VVector HalfEndToBeginningVect = EndToBeginningVect * (ActiveLength / 2);

                VVector a4Coords = a2Coords - HalfEndToBeginningVect;

                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a4").Any())
                {
                    ReferencePoint A4 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a4").First();
                    A4.ChangeLocation(planImage, a4Coords.x, a4Coords.y, a4Coords.z, stringBuilder);

                    DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A4.Id);

                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A4 not found.\n";


                }

                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a5").Any())
                {
                    VVector a5Coords = a2Coords - HalfEndToBeginningVect / 2;
                    ReferencePoint A5 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a5").First();
                    A5.ChangeLocation(planImage, a5Coords.x, a5Coords.y, a5Coords.z, stringBuilder);

                    DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A5.Id);

                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A5 not found.\n";

                }

                if (brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a6").Any())
                {
                    VVector a6Coords = a2Coords + HalfEndToBeginningVect / 2;
                    ReferencePoint A6 = brachyPlan.ReferencePoints.Where(c => c.Id.ToLower() == "a6").First();
                    A6.ChangeLocation(planImage, a6Coords.x, a6Coords.y, a6Coords.z, stringBuilder);

                    DisplayMessage += string.Format("Reference Point {0} found and location changed. \n", A6.Id);

                }
                else
                {
                    NoFindDisplayMessage += "Reference Point A6 not found.\n";

                }


            }
            else
            {
                NoFindDisplayMessage += "A4,A5, and A6 cannot be determined because A2 was not found.\n";
            }

            //dont need these right now
            //VVector a1Coords = a1.GetReferencePointLocation(planImage);
            //VVector a3Coords = a3.GetReferencePointLocation(planImage);



            if (NoFindDisplayMessage.Any() && DisplayMessage.Any())
            {
                MessageBox.Show(DisplayMessage + "\n" +NoFindDisplayMessage);
            }
            else if(DisplayMessage.Any())
            {
                MessageBox.Show(DisplayMessage);
            }

            

        }






        public static VVector GetPerpendicularVector(VVector v)
        {
            double x = v[0];
            double y = v[1];
            double z = v[2];

            VVector perpendicular = new VVector();
            perpendicular[0] = y;
            perpendicular[1] = -x;
            perpendicular[2] = 0;

            // Normalize the perpendicular vector
            double magnitude = Math.Sqrt(perpendicular[0] * perpendicular[0] + perpendicular[1] * perpendicular[1] + perpendicular[2] * perpendicular[2]);
            perpendicular[0] /= magnitude;
            perpendicular[1] /= magnitude;
            perpendicular[2] /= magnitude;

            return perpendicular;
        }

    }
}
