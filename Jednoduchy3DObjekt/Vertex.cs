using System.Runtime.InteropServices;   //Používa atribút StructLayout.
using SlimDX;

namespace Jednoduchy3DObjekt
{
    [StructLayout(LayoutKind.Sequential)]  //Atribút def. pre Vertex sekvenčné usporiadanie položiek.
    public struct Vertex
    {
        public Vector3 Position;
        public int Color;

        public Vertex(Vector3 position, int color)
        {
            this.Position = position;
            this.Color = color;
        }
    }
}
