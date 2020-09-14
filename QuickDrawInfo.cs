using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace QuickDraw
{
    public class QuickDrawInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "QuickDrawGH";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Properties.Resources.QDicons_DRAW;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "A plugin for using the Google QuickDraw dataset within Grasshopper.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("147da39e-0b7d-4f17-afc4-0d9de2a2dcf4");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "James Dalessandro";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "@jaymezd on instagram";
            }
        }
    }
}
