$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$code = @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;

public static class SonicConceptExport {

    static readonly Color[] Palette = MakePalette();
    static readonly Dictionary<int,Color> Cache = new Dictionary<int,Color>();
    static Color[] MakePalette() {
        string[] hex={"171C30","252C40","394052","535C6D","737D8E","A0A9B6","352323","523124","75432B","965A32","B97943","D59B5D","674D2E","896333","B38640","D4AA54","F1CE77","F6DCA0","373439","514C4D","686264","817A75","9F9789","B9B09C","D2C9AE","751F2B","AB2933","D6433B","F16A4C","FF6E60","FFD0BD","FFF4D6","D4D4CC","B2B5B5","90989E","617D91","8EADBE","B8CED7","DDEAF0","F5FAF6"};
        Color[] colors=new Color[hex.Length]; for(int i=0;i<hex.Length;i++) colors[i]=ColorTranslator.FromHtml("#"+hex[i]); return colors;
    }
    static Color Quantize(Color c) {
        if(c.A<200) return Color.Transparent;
        int key=c.ToArgb(); Color result; if(Cache.TryGetValue(key,out result)) return result;
        int dist=int.MaxValue; result=Palette[0];
        foreach(Color p in Palette) { int r=c.R-p.R,g=c.G-p.G,b=c.B-p.B; int d=r*r+g*g+b*b; if(d<dist){dist=d;result=p;} }
        Cache[key]=result; return result;
    }
    public static Rectangle Bounds(Bitmap b) {
        int l=b.Width,t=b.Height,r=-1,bot=-1;
        for(int y=0;y<b.Height;y++) for(int x=0;x<b.Width;x++) if(b.GetPixel(x,y).A>=200){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);}
        if(r<0) throw new Exception("Empty frame"); return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static Bitmap[] Cells(string path,int cols,int rows,int count) {
        Bitmap[] result=new Bitmap[count]; using(Bitmap s=new Bitmap(path)) {
            for(int i=0;i<count;i++) {
                int x0=(i%cols)*s.Width/cols,x1=(i%cols+1)*s.Width/cols;
                int y0=(i/cols)*s.Height/rows,y1=(i/cols+1)*s.Height/rows;
                result[i]=s.Clone(new Rectangle(x0,y0,x1-x0,y1-y0),PixelFormat.Format32bppArgb);
            }
        } return result;
    }
    static Bitmap Sample(Bitmap src,Rectangle crop,int width,int height) {
        Bitmap b=new Bitmap(width,height,PixelFormat.Format32bppArgb);
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) {
            int sx=crop.X+Math.Min(crop.Width-1,(int)((x+0.5)*crop.Width/width));
            int sy=crop.Y+Math.Min(crop.Height-1,(int)((y+0.5)*crop.Height/height));
            b.SetPixel(x,y,Quantize(src.GetPixel(sx,sy)));
        } return b;
    }
    static void Blit(Bitmap dst,Bitmap src,int dx,int dy) {
        for(int y=0;y<src.Height;y++) for(int x=0;x<src.Width;x++) {
            int tx=dx+x,ty=dy+y; if(tx<0||ty<0||tx>=dst.Width||ty>=dst.Height) continue;
            Color c=src.GetPixel(x,y); if(c.A>=200) dst.SetPixel(tx,ty,c);
        }
    }
    static void Patch(Bitmap dst,Bitmap src,Rectangle rect) {
        for(int y=rect.Top;y<rect.Bottom;y++) for(int x=rect.Left;x<rect.Right;x++) dst.SetPixel(x,y,src.GetPixel(x,y));
    }

    static Bitmap Identity(string path){
        using(Bitmap src=new Bitmap(path)){
            Rectangle r=Bounds(src);double scale=Math.Min(70.0/r.Height,76.0/r.Width);
            int w=(int)Math.Round(r.Width*scale),h=(int)Math.Round(r.Height*scale);
            Bitmap dst=new Bitmap(128,128);using(Bitmap small=Sample(src,r,w,h))Blit(dst,small,(128-w)/2,97-h);return dst;
        }
    }
    static Bitmap Wave(string path){
        using(Bitmap src=new Bitmap(path)){
            Bitmap b=new Bitmap(128,128);using(Bitmap fit=Sample(src,Bounds(src),88,48))Blit(b,fit,24,40);return b;
        }
    }
    static void Draw(Graphics g,Bitmap b,int x,int y,int scale){
        g.DrawImage(b,new Rectangle(x,y,b.Width*scale,b.Height*scale),0,0,b.Width,b.Height,GraphicsUnit.Pixel);
    }
    static void Compare(string root,Bitmap b){
        string generated=Path.GetFullPath(Path.Combine(root,"..","..","Assets","Art","Generated"));
        string[] ids={"CannonRapid","CannonBlast","CannonFrost","CannonBallista"};
        string[] labels={"RAPID","BLAST","FROST","BALLISTA","SONIC B - APPROVED"};
        using(Bitmap board=new Bitmap(1296,852))using(Graphics g=Graphics.FromImage(board))using(Font font=new Font("Arial",16)){
            g.Clear(Color.FromArgb(224,224,224));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            for(int i=0;i<5;i++){
                int x=i%3*432,y=i/3*426;g.DrawString(labels[i],font,Brushes.DarkSlateGray,x+16,y+12);
                if(i<4)using(Bitmap old=new Bitmap(Path.Combine(generated,ids[i],"identity","identity-1.png"))){Draw(g,old,x+(432-old.Width*3)/2,y+38,3);}
                else Draw(g,b,x+24,y+38,3);
            }board.Save(Path.Combine(root,"qc","compare.png"),ImageFormat.Png);
        }
    }
    static void Board(string root,Bitmap b,Bitmap normal,Bitmap shock,Bitmap ready){
        using(Bitmap board=new Bitmap(1296,852))using(Graphics g=Graphics.FromImage(board))using(Font f=new Font("Arial",16)){
            g.Clear(Color.FromArgb(224,224,224));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawString("SONIC B - APPROVED / LAMPS OFF",f,Brushes.DarkSlateGray,16,12);Draw(g,b,24,38,3);
            g.DrawString("SONIC B - 3 LAMPS / READY",f,Brushes.DarkSlateGray,448,12);Draw(g,ready,456,38,3);
            g.DrawString("SR - RHYTHM / KNOCKBACK",f,Brushes.DarkSlateGray,880,12);
            g.DrawString("1: NORMAL 20  -> LED 1\n2: NORMAL 20  -> LED 2\n3: NORMAL 20  -> LED 3\n4: SHOCK   35  -> OFF\n\n1 SHOT / SECOND\n1 HIT / ENEMY / SHOT\n\nSAME CONE / STRONGER PULSE\nPUSH BACK ALONG THE PATH\nSHARED PUSH IMMUNITY: 2s",f,Brushes.DarkSlateGray,880,80);
            g.DrawString("NORMAL - PALE BLUE / SILVER",f,Brushes.DarkSlateGray,16,438);Draw(g,normal,24,464,3);
            g.DrawString("4TH SHOT - GOLD / IVORY",f,Brushes.DarkSlateGray,448,438);Draw(g,shock,456,464,3);
            g.DrawString("APPROVED FORM B",f,Brushes.DarkSlateGray,880,438);
            g.DrawString("TWO STACKED HORN SPEAKERS\n\n3 NORMAL SHOTS -> 1 SHOCK\nLAMPS: 1 -> 2 -> 3 -> OFF\n\nNO ENEMY: KEEP RHYTHM\nNO COOLDOWN ANIMATION",f,Brushes.DarkSlateGray,880,500);
            board.Save(Path.Combine(root,"qc","concept-board.png"),ImageFormat.Png);
        }
    }
    static void InContext(string root,Bitmap body,Bitmap normal,Bitmap shock){
        using(Bitmap board=new Bitmap(1584,768))using(Graphics g=Graphics.FromImage(board))using(Font f=new Font("Arial",16)){
            g.Clear(Color.FromArgb(224,224,224));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            for(int row=0;row<2;row++){
                g.DrawString(row==0?"NORMAL PULSE - 20 DAMAGE":"4TH PULSE - 35 DAMAGE + KNOCKBACK / SAME CONE",f,Brushes.DarkSlateGray,16,row*384+12);
                // Preview positioning only; final animation emission anchor can be refined on approved B.
                Draw(g,body,1000,row*384,3);
                Draw(g,row==0?normal:shock,1000+32*3-112*3,row*384+(48-64)*3,3);
            }board.Save(Path.Combine(root,"qc","waves-in-context.png"),ImageFormat.Png);
        }
    }
    static string Check(Bitmap b,string label,bool body){
        Rectangle r=Bounds(b);HashSet<int> colors=new HashSet<int>();
        for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
            Color c=b.GetPixel(x,y);if(c.A!=0&&c.A!=255)throw new Exception("Nonbinary alpha "+label);
            if(c.A>0){if(c.R+c.G+c.B==0)throw new Exception("Pure black "+label);colors.Add(c.ToArgb());}
        }
        if(body&&(r.Bottom-1!=96||r.Width>76||r.Height>75))throw new Exception("Wrong body dimensions "+label);
        return String.Format("- {0}: canvas128x128; visible{1}x{2}; bottom y{3}; {4} colors; alpha0/255; no pure black.",label,r.Width,r.Height,r.Bottom-1,colors.Count);
    }
    public static void Run(string root){
                using(Bitmap b=Identity(Path.Combine(root,"raw","identity-b.png")))
        using(Bitmap ready=Identity(Path.Combine(root,"raw","identity-b-initial.png")))
        using(Bitmap normal=Wave(Path.Combine(root,"raw","wave-normal.png")))
        using(Bitmap shock=Wave(Path.Combine(root,"raw","wave-shock.png"))){
            b.Save(Path.Combine(root,"identity","identity-variant-b.png"),ImageFormat.Png);
            b.Save(Path.Combine(root,"identity","identity-1.png"),ImageFormat.Png);
            b.Save(Path.Combine(root,"identity","sheet-transparent.png"),ImageFormat.Png);
            normal.Save(Path.Combine(root,"concept","wave-normal.png"),ImageFormat.Png);
            shock.Save(Path.Combine(root,"concept","wave-shock.png"),ImageFormat.Png);
            ready.Save(Path.Combine(root,"concept","rhythm-ready-b.png"),ImageFormat.Png);
            using(Bitmap sheet=new Bitmap(256,128)){Blit(sheet,normal,0,0);Blit(sheet,shock,128,0);sheet.Save(Path.Combine(root,"concept","waves-transparent.png"),ImageFormat.Png);}
            Compare(root,b);Board(root,b,normal,shock,ready);InContext(root,b,normal,shock);
            List<string> report=new List<string>();report.Add("# Approved form B - Sonic concept validation");report.Add("");
            report.Add(Check(b,"Identity B",true));report.Add(Check(ready,"Ready B concept",true));
            report.Add(Check(normal,"Normal wave",false));report.Add(Check(shock,"Shock wave",false));
            report.Add("");report.Add("Body bottom y96. SourceAlpha retained; nearest-neighbor sampling and restricted palette. Four existing references at exactly3x in compare.png.");
            report.Add("Wave concept area88x48; emission origin(112,64), travelingLEFT in both assets. Matching footprint for same cone, different width/color of wavefronts.");
            report.Add("Ready body is a still concept from the initial source, not an animation frame registered to selected identity. Identity lamps edited OFF with image_gen.");
            report.Add("Owner selected form B. This report covers concept stills only. The full animation set is documented in HANDOFF.md and qc/validation-production.md. No cooldown or gameplay implementation.");
            File.WriteAllLines(Path.Combine(root,"qc","validation.md"),report,new UTF8Encoding(false));
        }
    }
}

'@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
[SonicConceptExport]::Run($root)
Get-Content -Encoding utf8 (Join-Path $root 'qc/validation.md')
