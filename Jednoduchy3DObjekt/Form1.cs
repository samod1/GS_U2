/*
 * Projekt je nastavený a odladený pre platformu x86 a .NET 4.0.
 * Ukážka obsahuje vytvorenie objektu Device a SwapChain spoločne jednou metódou CreateWithSwapChain a použitie komponentu Timer pre animáciu objektu.
 * Alt+Enter pre celoobrazovkový mód nefunguje v DXGI pre WinForms.
 * Tento kód nie je optimalizovaný a slúži len ako demo ukážka.
 */

using System;
using System.Drawing;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D11;
using DX11 = SlimDX.Direct3D11;
using DXGI = SlimDX.DXGI;

namespace Jednoduchy3DObjekt
{
    public partial class Form1 : Form
    {
        //Deklarácie globálnych premenných.
        private DX11.Device m_device;                       //Zariadenie D3D11.
        private DX11.DeviceContext m_deviceContext;         //Kontext zariadenia.
        private DX11.RenderTargetView m_renderTarget;       //Cieľ renderovania.
        private DXGI.SwapChain m_swapChain;
        private DXGI.SwapChainDescription m_swapChainDesc;
        private bool m_initialized;                         //Pomocná premenná s návratovou hodnotou inicializačnej metódy.
        private Objekt m_simpleBox;
        //Transformačné matice.
        private Matrix m_viewMatrix;
        private Matrix m_projMatrix;
        private Matrix m_worldMatrix;
        private Matrix m_viewProjMatrix;

        private float angle;                               //Premenná pre animáciu objektu.

        // Pre vytvorenie zariadenia akceptujeme DX10 a vyššiu verziu adaptéra.
        FeatureLevel[] m_levels = {
                            FeatureLevel.Level_11_0,
                            FeatureLevel.Level_10_1,
                            FeatureLevel.Level_10_0
                                };
        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.ResizeRedraw, true);          //Nastavenie správania sa okna ap.
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.Opaque, true);
            renderTimer.Start();
        }

        // Inicializácia zariadenia a ďalších potrebných zdrojov pre rendering. Metóda vráti true, ak skončí bez chyby.
        private bool Initialize3D()
        {
            try
            {     
                //Vytvorenie objektu typu SwapChainDescription a nastavenie jeho vlastností. 
                m_swapChainDesc = new DXGI.SwapChainDescription();
                m_swapChainDesc.OutputHandle = this.Handle;
                m_swapChainDesc.IsWindowed = true;
                m_swapChainDesc.BufferCount = 1;
                m_swapChainDesc.Flags = DXGI.SwapChainFlags.AllowModeSwitch;
                m_swapChainDesc.ModeDescription = new DXGI.ModeDescription(
                    this.Width,
                    this.Height,
                    new Rational(60, 1),
                    DXGI.Format.R8G8B8A8_UNorm);
                m_swapChainDesc.SampleDescription = new DXGI.SampleDescription(1, 0);
                m_swapChainDesc.SwapEffect = DXGI.SwapEffect.Discard;
                m_swapChainDesc.Usage = DXGI.Usage.RenderTargetOutput;

                ///Vytvorenie objektu zariadenie a SwapChain spoločne jednou metódou.
                DX11.Device.CreateWithSwapChain(
                    DriverType.Hardware,            //Typ renderovacieho zariadenia: SW renderer: Reference, Warp, Hardware - hardvérový renderer:  http://slimdx.org/tutorials/DeviceCreation.php.
                    DeviceCreationFlags.None,       //Žiadne extra návestia.
                    m_levels,                       //Kompatibilita s verziami DX.  
                    m_swapChainDesc,                //Objekt typu SwapChainDescription.
                    out m_device,                   //vráti Direct3D zariadenie reprezentujúce virtuálny graf. HW.
                    out m_swapChain);               //vráti SwapChain pre prácu s baframi.

                //Vytvorenie zobrazovacej plochy - celé okno ap.
                var m_viewPort = new Viewport(0.0f, 0.0f, this.ClientSize.Width, this.ClientSize.Height);

                //Vytvorenie objektu zadného bafra a cieľa renderovania.
                DX11.Texture2D m_backBuffer = Texture2D.FromSwapChain<Texture2D>(m_swapChain, 0);
                m_renderTarget = new RenderTargetView(m_device, m_backBuffer);
                        
                m_deviceContext = m_device.ImmediateContext;   //Nastavenie kontextu zariadenia, ktorý obsahuje nastavenia pre D3D zariadenie, tu priame renderovanie bez vytvárania príkazového zoznamu. Pozri: http://msdn.microsoft.com/en-us/library/windows/desktop/ff476880%28v=vs.85%29.aspx. 
                m_deviceContext.Rasterizer.SetViewports(m_viewPort);  //Nastavenie zobrazovacej plochy.
                m_deviceContext.OutputMerger.SetTargets(m_renderTarget);  //Nastavenie cieľa renderovania.
                
                m_initialized = true;           
            }                  
            catch (Exception ex)
            {
                MessageBox.Show("Chyba počas inicializácie Direct3D11: \n" + ex.Message);
                m_initialized = false;
            }

            return m_initialized;
        }

       // Alt 1: Volanie inicializačnej metódy na udalosť okna OnLoad.
       protected override void OnLoad(EventArgs e)
        {
            //base.OnLoad(e);
            Initialize3D();
        }
       

       // Alt2: Rendering na udalosť OnPaint
       /* protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (m_initialized)
            {
                //Definovanie pohľadu na scénu: transformácia z WCS do súradnicového systému kamery
                m_viewMatrix = Matrix.LookAtLH(
                    new Vector3(0f, 0f, -4f),       //poloha kamery
                    new Vector3(0f, 0f, 1f),        //smer pohľadu kamery
                    new Vector3(0f, 1f, 0f));       //tzv. UpVector definujúci smer hore

                //Definovanie typu premietania pohľadu scény
                m_projMatrix = Matrix.PerspectiveFovLH((float)Math.PI * 0.5f, this.Width / (float)this.Height, 0.1f, 100f);

                //Výsledná zobrazovacia matica
                m_viewProjMatrix = m_viewMatrix * m_projMatrix;

                //Transform8cia objektu do WCS objektu
                m_worldMatrix = Matrix.RotationYawPitchRoll(0.85f, 0.85f, 0.0f);                     //Rot. okolo osi y, rot. okolo osi x,rot okolo osi z. 
                m_simpleBox = new SimpleBox();
                m_simpleBox.LoadResources(m_device);                                                 //Zavedenie zdrojov používateľskou metódou.
                m_deviceContext.ClearRenderTargetView(m_renderTarget, new Color4(Color.Black));      //Farba pozadia.
                m_simpleBox.Render(m_device, m_worldMatrix, m_viewProjMatrix);                       //Rendering scény.
                m_swapChain.Present(0, DXGI.PresentFlags.None);                                      //Zobrazenie scény.
                ReleaseObjects();                     
            }
        } */
        
       
        void ReleaseObjects()
        {
            //renderTimer.Stop();
            //Uvolnenie vytvorených COM objektov.
            m_swapChain.Dispose();
            m_device.Dispose();
            m_renderTarget.Dispose();
            //TODO: ďalšie objekty uvolniť, až kým v okne Output sa nezobrazí správa: Total of 0 objects still alive.
        }

        private void renderTimer_Tick(object sender, EventArgs e)
        {
            if (m_initialized)
            {
                m_simpleBox = new Objekt();
                m_simpleBox.LoadResources(m_device);   //Zavedenie zdrojov používateľskou metódou.

                //Nastavenie uhľa rotácie v závislosti od času behu aplikácie
                int time = Environment.TickCount / 10 % 1000;            //Čas v msek od spustenia ap.-umožňuje nastaviť rýchlosť animácie.  
                angle = (float)(time * (2.0f * Math.PI) / 1000.0f);      //Otáčanie CW.

                //angle = (float)(-(time * (2.0f * Math.PI) / 1000.0f)); //Otáčanie CCW.

                //Nastavenie transformačných matíc. 
                m_worldMatrix = Matrix.RotationX(0) * Matrix.RotationY(angle) * Matrix.RotationZ(0);  //Objekt bude rotovať okolo osi y v MCS
                //m_worldMatrix = Matrix.RotationX(angle) * Matrix.RotationY(angle) * Matrix.RotationZ(angle);  //Objekt bude rotovať okolo všetkých osí v MCS.
                //m_worldMatrix = Matrix.Translation(0f, 0f, 1f) * Matrix.RotationX(0) * Matrix.RotationY(angle) * Matrix.RotationZ(0);  //Objekt sa otáča okolo osi y posunutej o (0,0,1).
                //m_worldMatrix = Matrix.Scaling(1f,1f,0.5f) * Matrix.Translation(0f,0f,0f) * Matrix.RotationX(0) * Matrix.RotationY(angle) * Matrix.RotationZ(0);  //Objekt sa zobrazí ako kváder.

                //Definovanie pohľadu na scénu: transformácia z WCS do súradnicového systému kamery.
                m_viewMatrix = Matrix.LookAtLH(
                     new Vector3(0f, 0f, -3f),       //Poloha kamery.
                     new Vector3(0f, 0f, 1f),        //Smer pohľadu kamery.
                     new Vector3(0f, 1f, 0f));       //Tzv. UpVector definujúci smer hore.

                //Definovanie typu premietania pohľadu scény.
                m_projMatrix = Matrix.PerspectiveFovLH((float)Math.PI * 0.5f, this.Width / (float)this.Height, 0.1f, 100f);

                //Výsledná zobrazovacia matica.
                m_viewProjMatrix = m_viewMatrix * m_projMatrix;

                m_deviceContext.ClearRenderTargetView(m_renderTarget, new Color4(Color.Black));   //Farba pozadia pre renderovac9 cieľ - tu okno.
                m_simpleBox.Render(m_device, m_worldMatrix, m_viewProjMatrix);                    //Volanie používateľskej metódy Render.
                m_swapChain.Present(0, DXGI.PresentFlags.None);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(checkBox1.Checked == true)
            {
                Console.WriteLine("Kocka");
                //tu zobraz kocka 
            }

            else
            {
                if(checkBox2.Checked == true)
                {
                    Console.WriteLine("Gula");
                    // tu zobraz gulu. 
                }
            }

            if (checkBox1.Checked == true && checkBox2.Checked == true)
            {
                //Vratit chybu a zhodit appku
                MessageBox.Show("Musi byt oznacena iba jedna polozka", "Chyba",
                MessageBoxButtons.OK, MessageBoxIcon.Error);           
            }

        }
    }
}
