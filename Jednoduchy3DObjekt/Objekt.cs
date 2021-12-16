/*
 * Trieda Objekt obsahuje definíciu kocky s explicitnou inicializáciou matíc vrcholov a indexov.
 */ 

using System;
using System.Runtime.InteropServices;
using System.Drawing;
using SlimDX;
using DX11 = SlimDX.Direct3D11;
using DXGI = SlimDX.DXGI;
using D3DCompiler = SlimDX.D3DCompiler;

namespace Jednoduchy3DObjekt
{
    public class Objekt
    {
        //Globálne premenné.
        private DX11.Buffer m_vertexBuffer;
        private DX11.Buffer m_indexBuffer;
        private DX11.InputLayout m_vertexLayout;
        private DX11.Effect m_effect;
        private DX11.EffectTechnique m_effectTechnique;
        private DX11.EffectPass m_effectPass;
        private DX11.EffectMatrixVariable m_transformVariable;

       
        // Zavedenie zdrojov.
        public void LoadResources(DX11.Device device)
        {
            Vertex[] vertices = new Vertex[]   //Matica 8 vrcholov.
            {
                new Vertex(new Vector3(-1, -1, -1.5f), Color.Red.ToArgb()),
                new Vertex(new Vector3(1, -1, -1.5f), Color.LightBlue.ToArgb()),
                new Vertex(new Vector3(1, -1, 1.5f), Color.LightCyan.ToArgb()),
                new Vertex(new Vector3(-1, -1, 1), Color.CadetBlue.ToArgb()),
                new Vertex(new Vector3(-1, 1, -1.5f), Color.Red.ToArgb()),
                new Vertex(new Vector3(1,1,-1.5f), Color.Orange.ToArgb()),
                new Vertex(new Vector3(1,1,1), Color.Goldenrod.ToArgb()),
                new Vertex(new Vector3(-1, 1,1), Color.Yellow.ToArgb())
            };

            short[] indices = new short[]    //Matica 36 indexov.
            {
                4,1,0,4,5,1,   
                5,2,1,5,6,2,
                6,3,2,6,7,3,
                7,0,3,7,4,0,
                7,5,4,7,6,5,
                2,3,0,2,0,1
            };

            DataStream outStream = new DataStream(8 * Marshal.SizeOf(typeof(Vertex)), true, true);       //Inicializácia objektu typu DataStream pre vertex buffer.
            DataStream outStreamIndex = new DataStream(36 * Marshal.SizeOf(typeof(short)), true, true);  //Index buffer.

            for (int loop = 0; loop < vertices.Length; loop++)   //Načítanie hodnôt.
            {
                outStream.Write<Vertex>(vertices[loop]);
            }

            for (int loop = 0; loop < indices.Length; loop++)     //Načítanie hodnôt.  
            {
                outStreamIndex.Write<short>(indices[loop]);
            }

            outStream.Position = 0;        //Nastavenie polohy. 
            outStreamIndex.Position = 0;

            DX11.BufferDescription bufferDescription = new DX11.BufferDescription();
            bufferDescription.BindFlags = DX11.BindFlags.VertexBuffer;
            bufferDescription.CpuAccessFlags = DX11.CpuAccessFlags.None;
            bufferDescription.OptionFlags = DX11.ResourceOptionFlags.None;
            bufferDescription.SizeInBytes = 8 * Marshal.SizeOf(typeof(Vertex));
            bufferDescription.Usage = DX11.ResourceUsage.Default;

            DX11.BufferDescription bufferDescriptionIndex = new DX11.BufferDescription();
            bufferDescriptionIndex.BindFlags = DX11.BindFlags.IndexBuffer;
            bufferDescriptionIndex.CpuAccessFlags = DX11.CpuAccessFlags.None;
            bufferDescriptionIndex.OptionFlags = DX11.ResourceOptionFlags.None;
            bufferDescriptionIndex.SizeInBytes = 36 * Marshal.SizeOf(typeof(short));
            bufferDescriptionIndex.Usage = DX11.ResourceUsage.Default;

            m_vertexBuffer = new DX11.Buffer(device, outStream, bufferDescription);           //Inicializácia pre vertex buffer. 
            m_indexBuffer = new DX11.Buffer(device, outStreamIndex, bufferDescriptionIndex);  //Index buffer.
            outStream.Close();
            outStreamIndex.Close();

            //Preloženie efektového súboru (HLSL) a priradenie objektu m_ShaderByteCode.
            D3DCompiler.ShaderBytecode m_shaderByteCode =
                    D3DCompiler.ShaderBytecode.CompileFromFile("Shaders/SimpleRendering.fx", 
                    "fx_5_0",                     
                    D3DCompiler.ShaderFlags.None, 
                    D3DCompiler.EffectFlags.None);

            m_effect = new DX11.Effect(device, m_shaderByteCode);   //Objekt pre správu množiny stavovývh objektov, zdrojov a šejdrov pre implementáciu D3D renderovacieho efektu.
            m_effectTechnique = m_effect.GetTechniqueByIndex(0);    //Získať techniku cez index.
            m_effectPass = m_effectTechnique.GetPassByIndex(0);     //Získať prechod cez index.
            m_transformVariable = m_effect.GetVariableByName("WorldViewProj").AsMatrix();   //Získanie premennej cez meno.

            DX11.InputElement[] m_inputElements = new DX11.InputElement[]     //Parameter pre metódu InputLayout.
            {
                new DX11.InputElement("POSITION",0,SlimDX.DXGI.Format.R32G32B32_Float,0,0),
                new DX11.InputElement("COLOR",0,SlimDX.DXGI.Format.R8G8B8A8_UNorm,12,0)
            };
            m_vertexLayout = new DX11.InputLayout(device, m_effectPass.Description.Signature, m_inputElements); //Vytvorenie objektu pre opis vstupu pre "Input assembler stage".
        }


        public void Render(DX11.Device device, Matrix world, Matrix viewProj)
        {
            DX11.DeviceContext m_deviceContext = device.ImmediateContext;     

            m_deviceContext.InputAssembler.InputLayout = m_vertexLayout;    //Priradenie opisu dát pre "Input assembler stage".
            m_deviceContext.InputAssembler.PrimitiveTopology = DX11.PrimitiveTopology.TriangleList;   //Spôsob interpretácie vrcholov ako zoznam 3-uholníkov.
            m_deviceContext.InputAssembler.SetIndexBuffer(m_indexBuffer, DXGI.Format.R16_UInt, 0);    //Index buffer priradený k "Input assembler stage". 
            m_deviceContext.InputAssembler.SetVertexBuffers(0, new DX11.VertexBufferBinding(m_vertexBuffer, Marshal.SizeOf(typeof(Vertex)), 0));
            m_transformVariable.SetMatrix(world * viewProj);   //Nastavenie združenej transformačnej matice pre transform. súradníc vrcholov do obrazovkových.
            m_effectPass.Apply(m_deviceContext);

            m_deviceContext.DrawIndexed(36, 0, 0);   //Renderovanie indexovaných dát.
        }
          
    }

}
