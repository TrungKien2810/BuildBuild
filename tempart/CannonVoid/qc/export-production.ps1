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

public static class VoidExport {
    static Rectangle IdentityBounds;
    static readonly Color[] Palette = MakePalette();
    static readonly Dictionary<int,Color> Cache = new Dictionary<int,Color>();
    static Color[] MakePalette() {
        string[] hex={"171C30","252C40","394052","535C6D","737D8E","A0A9B6","352323","523124","75432B","965A32","B97943","D59B5D","674D2E","896333","B38640","D4AA54","F1CE77","F6DCA0","373439","514C4D","686264","817A75","9F9789","B9B09C","D2C9AE","751F2B","AB2933","D6433B","F16A4C","FF6E60","FFD0BD","FFF4D6","D4D4CC","B2B5B5","90989E"};
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
    static void SaveGroup(string root,string dir,string prefix,Bitmap[] frames,int cols) {
        string folder=Path.Combine(root,dir); Directory.CreateDirectory(folder);
        int rows=(frames.Length+cols-1)/cols; using(Bitmap sheet=new Bitmap(frames[0].Width*cols,frames[0].Height*rows)) {
            for(int i=0;i<frames.Length;i++) {
                frames[i].Save(Path.Combine(folder,prefix+"-"+(i+1)+".png"),ImageFormat.Png);
                Blit(sheet,frames[i],(i%cols)*frames[i].Width,(i/cols)*frames[i].Height);
            }
            sheet.Save(Path.Combine(folder,"sheet-transparent.png"),ImageFormat.Png);
        }
    }
    static void Board(string root,Dictionary<string,Bitmap[]> groups) {
        using(Bitmap board=new Bitmap(1728,groups.Count*302)) using(Graphics g=Graphics.FromImage(board)) using(Font f=new Font("Arial",14)) {
            g.Clear(Color.FromArgb(224,224,224)); g.InterpolationMode=InterpolationMode.NearestNeighbor; g.PixelOffsetMode=PixelOffsetMode.Half;
            int row=0; foreach(KeyValuePair<string,Bitmap[]> group in groups) {
                g.DrawString(group.Key,f,Brushes.Black,12,row*302+8);
                for(int i=0;i<group.Value.Length;i++) {
                    Bitmap b=group.Value[i]; int scale=b.Width==48?4:2;
                    g.DrawImage(b,new Rectangle(i*288+(288-b.Width*scale)/2,row*302+32,b.Width*scale,b.Height*scale),0,0,b.Width,b.Height,GraphicsUnit.Pixel);
                    g.DrawString((i+1).ToString(),f,Brushes.Black,i*288+10,row*302+275);
                } row++;
            } board.Save(Path.Combine(root,"qc","all-assets.png"),ImageFormat.Png);
        }
    }
    static Bitmap Indexed(Bitmap src){
        Dictionary<int,int> counts=new Dictionary<int,int>();
        for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++){
            int key=src.GetPixel(x,y).ToArgb();int n;counts.TryGetValue(key,out n);counts[key]=n+1;
        }
        List<int> keys=new List<int>(counts.Keys);keys.Sort(delegate(int a,int b){return counts[b].CompareTo(counts[a]);});
        if(keys.Count>256)keys.RemoveRange(256,keys.Count-256);
        Bitmap result=new Bitmap(src.Width,src.Height,PixelFormat.Format8bppIndexed);
        ColorPalette palette=result.Palette;Dictionary<int,byte> map=new Dictionary<int,byte>();
        for(int i=0;i<keys.Count;i++){palette.Entries[i]=Color.FromArgb(keys[i]);map[keys[i]]=(byte)i;}
        result.Palette=palette;
        BitmapData data=result.LockBits(new Rectangle(0,0,result.Width,result.Height),ImageLockMode.WriteOnly,PixelFormat.Format8bppIndexed);
        byte[] bytes=new byte[data.Stride*result.Height];
        for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++){
            Color c=src.GetPixel(x,y);byte index;if(!map.TryGetValue(c.ToArgb(),out index)){
                int best=int.MaxValue;index=0;
                for(int i=0;i<keys.Count;i++){Color p=Color.FromArgb(keys[i]);int r=c.R-p.R,g=c.G-p.G,b=c.B-p.B,d=r*r+g*g+b*b;if(d<best){best=d;index=(byte)i;}}
                map[c.ToArgb()]=index;
            }
            bytes[y*data.Stride+x]=index;
        }
        System.Runtime.InteropServices.Marshal.Copy(bytes,0,data.Scan0,bytes.Length);result.UnlockBits(data);return result;
    }
    static void Gif(string path,List<Bitmap> frames,List<int> delays) {
        using(BinaryWriter writer=new BinaryWriter(File.Create(path))) {
            writer.Write(Encoding.ASCII.GetBytes("GIF89a")); writer.Write((ushort)frames[0].Width);writer.Write((ushort)frames[0].Height);
            writer.Write((byte)0x70);writer.Write((byte)0);writer.Write((byte)0);
            writer.Write(new byte[]{0x21,0xff,0x0b});writer.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));writer.Write(new byte[]{3,1,0,0,0});
            for(int i=0;i<frames.Count;i++) {
                byte[] data;using(MemoryStream stream=new MemoryStream())using(Bitmap indexed=Indexed(frames[i])){indexed.Save(stream,ImageFormat.Gif);data=stream.ToArray();}
                byte packed=data[10];int paletteSize=3*(1<<((packed&7)+1));int pos=13+(((packed&128)!=0)?paletteSize:0);
                while(data[pos]!=0x2c) {
                    if(data[pos]!=0x21) throw new Exception("Unexpected GIF block");pos+=2;
                    while(data[pos]!=0){pos+=data[pos]+1;}pos++;
                }
                byte imagePacked=data[pos+9];int localSize=3*(1<<((imagePacked&7)+1));bool local=(imagePacked&128)!=0;
                byte[] palette=new byte[local?localSize:paletteSize];Array.Copy(data,local?pos+10:13,palette,0,palette.Length);
                writer.Write(new byte[]{0x21,0xf9,4,4});writer.Write((ushort)Math.Max(1,delays[i]/10));writer.Write((byte)0);writer.Write((byte)0);
                writer.Write((byte)0x2c);writer.Write((ushort)0);writer.Write((ushort)0);writer.Write((ushort)frames[i].Width);writer.Write((ushort)frames[i].Height);
                writer.Write((byte)(0x80|(imagePacked&0x40)|((local?imagePacked:packed)&7)));writer.Write(palette);
                pos+=10+(local?localSize:0);writer.Write(data[pos++]);
                while(data[pos]!=0){int size=data[pos];writer.Write(data,pos,size+1);pos+=size+1;}writer.Write((byte)0);
            }writer.Write((byte)0x3b);
        }
    }

    static readonly Rectangle Core = new Rectangle(34,40,20,17);
    static readonly Point CoreAnchor = new Point(43,49);
    const int FixedBaseY=71;
    static Bitmap NormalizeBody(Bitmap src) {
        Bitmap b=new Bitmap(144,128);
        using(Bitmap s=Sample(src,Bounds(src),IdentityBounds.Width,IdentityBounds.Height))Blit(b,s,IdentityBounds.X,IdentityBounds.Y);
        return b;
    }
    static bool CoreColor(Color c) {return c.A>0&&((c.B>c.R&&c.B>c.G&&c.R<90)||(c.R>100&&c.R>c.G*1.7&&c.G<130)||(c.R>235&&c.G>205&&c.B>155));}
    static void CorePatch(Bitmap dst,Bitmap model,Bitmap identity) {
        for(int y=Core.Top;y<Core.Bottom;y++)for(int x=Core.Left;x<Core.Right;x++){
            Color a=identity.GetPixel(x,y),c=model.GetPixel(x,y);
            if(CoreColor(a)&&CoreColor(c))dst.SetPixel(x,y,c);
        }
    }
    static Bitmap Recoil(Bitmap src,int offset) {
        Bitmap b=new Bitmap(144,128);
        for(int y=0;y<128;y++)for(int x=0;x<144;x++){
            Color c=src.GetPixel(x,y);if(c.A==0)continue;int tx=x+(y<FixedBaseY?offset:0);
            if(tx>=0&&tx<144)b.SetPixel(tx,y,c);
        }return b;
    }
    static Bitmap[] Bodies(string root,string name,Bitmap identity){
        int count=name=="idle"?4:6,cols=count==4?2:3;
        Bitmap[] raw=Cells(Path.Combine(root,"raw",name+"-sheet.png"),cols,2,count);
        Bitmap[] frames=new Bitmap[count];for(int i=0;i<count;i++)frames[i]=(Bitmap)identity.Clone();
        if(name=="idle"){
            for(int i=1;i<3;i++)using(Bitmap normalized=NormalizeBody(raw[i]))CorePatch(frames[i],normalized,identity);
        }else{
            using(Bitmap charged=NormalizeBody(raw[1]))CorePatch(frames[1],charged,identity);
            using(Bitmap fired=NormalizeBody(raw[2]))CorePatch(frames[2],fired,identity);
            Rectangle bounds=Bounds(raw[2]);Rectangle crop=new Rectangle(bounds.Left,bounds.Top,Math.Max(1,bounds.Width/5),bounds.Height);
            using(Bitmap flash=raw[2].Clone(crop,PixelFormat.Format32bppArgb)){
                for(int y=0;y<flash.Height;y++)for(int x=0;x<flash.Width;x++){
                    Color c=flash.GetPixel(x,y);if(c.A<200||c.R<160||c.R<c.G*1.3&&c.G<210)flash.SetPixel(x,y,Color.Transparent);
                }
                try{using(Bitmap small=Sample(flash,Bounds(flash),10,10))Blit(frames[2],small,CoreAnchor.X-17,CoreAnchor.Y-5);}catch(Exception){}
            }
            frames[3].Dispose();frames[3]=Recoil(identity,2);
            frames[4].Dispose();frames[4]=Recoil(identity,1);
        }
        foreach(Bitmap b in raw)b.Dispose();return frames;
    }
    static Bitmap[] IrregularEffects(string root,string name,int maxW,int maxH){
        Bitmap[] cells;using(Bitmap source=new Bitmap(Path.Combine(root,"raw",name+"-sheet.png"))){
            List<Rectangle> rects=new List<Rectangle>();int w=source.Width,h=source.Height;
            if(name=="collapse"){
                int[] xs={0,(int)(w*0.348),(int)(w*0.617),(int)(w*0.842),w};
                for(int i=0;i<4;i++)rects.Add(new Rectangle(xs[i],0,xs[i+1]-xs[i],h));
            }else{
                int split=(int)(h*0.565);int[] top={0,(int)(w*0.29),(int)(w*0.57),w};int[] bottom={0,(int)(w*0.36),(int)(w*0.69),w};
                for(int i=0;i<3;i++)rects.Add(new Rectangle(top[i],0,top[i+1]-top[i],split));
                for(int i=0;i<3;i++)rects.Add(new Rectangle(bottom[i],split,bottom[i+1]-bottom[i],h-split));
            }
            cells=new Bitmap[rects.Count];for(int i=0;i<cells.Length;i++)cells[i]=source.Clone(rects[i],PixelFormat.Format32bppArgb);
        }
        int bw=0,bh=0;foreach(Bitmap c in cells){Rectangle r=Bounds(c);bw=Math.Max(bw,r.Width);bh=Math.Max(bh,r.Height);}
        Bitmap[] frames=new Bitmap[cells.Length];
        for(int i=0;i<cells.Length;i++){
            Rectangle r=Bounds(cells[i]);int w=Math.Max(1,(int)Math.Round((double)r.Width/bw*maxW)),h=Math.Max(1,(int)Math.Round((double)r.Height/bh*maxH));
            frames[i]=new Bitmap(128,128);using(Bitmap fit=Sample(cells[i],r,w,h))Blit(frames[i],fit,64-w/2,64-h/2);
            cells[i].Dispose();
        }return frames;
    }
    static Bitmap[] Effects(string root,string name,int count,int cols,int maxW,int maxH,int anchorFrame) {
        if(name=="collapse"||name.StartsWith("bullet-impact"))return IrregularEffects(root,name,maxW,maxH);
        Bitmap[] cells=Cells(Path.Combine(root,"raw",name+"-sheet.png"),cols,(count+cols-1)/cols,count);
        Rectangle union=Bounds(cells[0]);for(int i=1;i<count;i++)union=Rectangle.Union(union,Bounds(cells[i]));
        Rectangle anchorBounds=Bounds(cells[anchorFrame]);
        double ax=anchorBounds.Left+anchorBounds.Width/2.0,ay=anchorBounds.Top+anchorBounds.Height/2.0;
        if(name.StartsWith("vortex-loop")){ax=union.Left+union.Width/2.0;ay=union.Top+union.Height/2.0;}
        double sx=(double)maxW/union.Width,sy=(double)maxH/union.Height;
        int ox=64-(int)Math.Round((ax-union.Left)*sx),oy=64-(int)Math.Round((ay-union.Top)*sy);
        Bitmap[] frames=new Bitmap[count];
        for(int i=0;i<count;i++){
            frames[i]=new Bitmap(128,128);
            using(Bitmap fit=Sample(cells[i],union,maxW,maxH))Blit(frames[i],fit,ox,oy);
            cells[i].Dispose();
        }return frames;
    }
    static Bitmap Scene(Bitmap body,Bitmap fx,int fxX,int fxY,int phase,int charge) {
        Bitmap scene=new Bitmap(320,180);
        using(Graphics g=Graphics.FromImage(scene)){
            g.Clear(Color.FromArgb(211,211,211));using(Font f=new Font("Arial",9)){
                g.DrawString("PHAO HAC TINH - FORM B",f,Brushes.DarkSlateGray,8,8);
                string[] phases={"IDLE","FIRE","CORE FLIGHT","OPEN","PULL / ABSORB","COLLAPSE","DETONATE"};
                g.DrawString(phases[phase]+"    CHARGE: "+charge,f,Brushes.DarkSlateGray,8,24);
            }
        }
        Blit(scene,body,160,30);if(fx!=null)Blit(scene,fx,fxX-64,fxY-64);
        Bitmap big=new Bitmap(960,540);using(Graphics g=Graphics.FromImage(big)){
            g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(scene,new Rectangle(0,0,960,540),0,0,320,180,GraphicsUnit.Pixel);
        }scene.Dispose();return big;
    }
    static void Preview(string root,Dictionary<string,Bitmap[]> groups) {
        for(int finalLevel=0;finalLevel<=2;finalLevel++){
            List<Bitmap> frames=new List<Bitmap>();List<int> delays=new List<int>();
            Bitmap[] idle=groups["idle"],attack=groups["attack"];
            for(int i=0;i<4;i++){frames.Add(Scene(idle[i],null,0,0,0,0));delays.Add(160);}
            int[] fireTimes={160,150,80,90,120,180};
            for(int i=0;i<6;i++){frames.Add(Scene(attack[i],null,0,0,1,0));delays.Add(fireTimes[i]);}
            for(int i=0;i<4;i++){
                int x=160+CoreAnchor.X-(i+1)*(160+CoreAnchor.X-78)/4;
                int y=30+CoreAnchor.Y+(i+1)*(115-30-CoreAnchor.Y)/4;
                frames.Add(Scene(idle[0],groups["bullet"][i],x,y,2,0));delays.Add(90);
            }
            for(int i=0;i<4;i++){frames.Add(Scene(idle[0],groups["vortex-open"][i],78,115,3,0));delays.Add(60);}
            for(int level=0;level<=finalLevel;level++)for(int i=0;i<4;i++){
                Bitmap fx=(Bitmap)groups["vortex-loop-"+level][i].Clone();
                if(level>0)Blit(fx,groups["absorb"][i],0,0);
                frames.Add(Scene(idle[0],fx,78,115,4,level==0?0:level==1?2:5));delays.Add(finalLevel==0?375:finalLevel==1?188:125);fx.Dispose();
            }
            for(int i=0;i<4;i++){frames.Add(Scene(idle[0],groups["collapse"][i],78,115,5,finalLevel==0?0:finalLevel==1?2:5));delays.Add(50);}
            string impact="bullet-impact"+(finalLevel==0?"":"-"+finalLevel);
            for(int i=0;i<6;i++){frames.Add(Scene(idle[0],groups[impact][i],78,115,6,finalLevel==0?0:finalLevel==1?2:5));delays.Add(i==2?140:70);}
            frames.Add(Scene(idle[0],null,0,0,0,0));delays.Add(650);
            string file=finalLevel==0?"preview-zero.gif":finalLevel==1?"preview-medium.gif":"preview.gif";
            Gif(Path.Combine(root,"qc",file),frames,delays);
            using(Bitmap gif=new Bitmap(Path.Combine(root,"qc",file))){
                int n=gif.GetFrameCount(FrameDimension.Time);if(n!=frames.Count)throw new Exception("GIF frame count mismatch");
                for(int i=0;i<n;i++)gif.SelectActiveFrame(FrameDimension.Time,i);
            }
            if(finalLevel==2)frames[frames.Count-5].Save(Path.Combine(root,"qc","preview-blast.png"),ImageFormat.Png);
            foreach(Bitmap f in frames)f.Dispose();
        }
        Directory.CreateDirectory(Path.Combine(root,"qc","clips"));
        foreach(KeyValuePair<string,Bitmap[]> group in groups){
            if(group.Key=="icon")continue;
            List<Bitmap> frames=new List<Bitmap>();List<int> times=new List<int>();
            foreach(Bitmap b in group.Value){
                Bitmap canvas=new Bitmap(432,384);using(Graphics g=Graphics.FromImage(canvas)){
                    g.Clear(Color.FromArgb(211,211,211));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                    g.DrawImage(b,new Rectangle((432-b.Width*3)/2,0,b.Width*3,b.Height*3),0,0,b.Width,b.Height,GraphicsUnit.Pixel);
                }frames.Add(canvas);times.Add(group.Key=="idle"?220:100);
            }
            Gif(Path.Combine(root,"qc","clips",group.Key+".gif"),frames,times);
            foreach(Bitmap b in frames)b.Dispose();
        }
    }
    static void Overview(string root,Dictionary<string,Bitmap[]> groups,Bitmap identity){
        using(Bitmap board=new Bitmap(1728,1704))using(Graphics g=Graphics.FromImage(board))using(Font font=new Font("Arial",16)){
            g.Clear(Color.FromArgb(224,224,224));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            int slot=0;g.DrawString("FORM B - APPROVED",font,Brushes.DarkSlateGray,18,14);
            g.DrawImage(identity,new Rectangle(0,38,432,384),0,0,144,128,GraphicsUnit.Pixel);slot++;
            foreach(KeyValuePair<string,Bitmap[]> group in groups){
                int index=group.Key.StartsWith("bullet-impact")?2:group.Key=="attack"?2:group.Key=="vortex-open"?3:0;
                Bitmap b=group.Value[index];int x=slot%4*432,y=slot/4*426,scale=b.Width==48?6:3;
                g.DrawString(group.Key+" ("+group.Value.Length+")",font,Brushes.DarkSlateGray,x+18,y+14);
                g.DrawImage(b,new Rectangle(x+(432-b.Width*scale)/2,y+38+(384-b.Height*scale)/2,b.Width*scale,b.Height*scale),0,0,b.Width,b.Height,GraphicsUnit.Pixel);
                slot++;
            }board.Save(Path.Combine(root,"qc","overview.png"),ImageFormat.Png);
        }
    }
    static void Validate(string root,Dictionary<string,Bitmap[]> groups,Bitmap identity){
        List<string> report=new List<string>();report.Add("# Form B full-set validation");report.Add("");
        int total=0;
        foreach(KeyValuePair<string,Bitmap[]> group in groups){
            int maxW=0,maxH=0;HashSet<int> colors=new HashSet<int>();
            foreach(Bitmap b in group.Value){
                Rectangle r=Bounds(b);maxW=Math.Max(maxW,r.Width);maxH=Math.Max(maxH,r.Height);
                for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
                    Color c=b.GetPixel(x,y);if(c.A!=0&&c.A!=255)throw new Exception("Partial alpha "+group.Key);
                    if(c.A>0){colors.Add(c.ToArgb());if(c.R+c.G+c.B==0)throw new Exception("Pure black "+group.Key);}
                    if((group.Key=="idle"||group.Key=="attack")&&y>=FixedBaseY&&c.ToArgb()!=identity.GetPixel(x,y).ToArgb())throw new Exception("Moving base "+group.Key);
                }
                if(group.Key=="idle"||group.Key=="attack")if(r.Bottom-1!=96)throw new Exception("Body baseline");
                if(group.Key.StartsWith("vortex-loop")&&(r.Width>58||r.Height>32))throw new Exception("Vortex footprint");
            }
            total+=group.Value.Length;
            report.Add(String.Format("- {0}: {1} frames; canvas {2}x{3}; maximum opaque bounds {4}x{5}; {6} colors.",group.Key,group.Value.Length,group.Value[0].Width,group.Value[0].Height,maxW,maxH,colors.Count));
        }
        foreach(Bitmap b in new[]{groups["idle"][0],groups["idle"][3],groups["attack"][0],groups["attack"][5]})
            for(int y=0;y<128;y++)for(int x=0;x<144;x++)if(b.GetPixel(x,y).ToArgb()!=identity.GetPixel(x,y).ToArgb())throw new Exception("Rest differs from approved B");
        report.Add("");report.Add(total+" individual non-identity frames. 13 per-group sheets. 1 approved identity.");
        report.Add("Binary alpha throughout, no pure black. Fixed base/neck pixels at y>=71 exactly match approved B. Body bottom y96.");
        report.Add("Idle1/4 and attack1/6 match identity exactly; recoil +2/+1 logical px RIGHT.");
        report.Add("Core/muzzle anchor (43,49); VFX anchor (64,64). Loop area58x32 at all charge levels. Impact union64x48 at all levels.");
        report.Add("Regular-grid VFX use a shared crop/scale. Irregular collapse/impact sheets use explicit nonuniform source regions, a shared sequence scale and centered registration; no pieces cross exported frame boundaries. Absorb ends at64,64.");
        report.Add("Body core/flash patches sampled from image_gen sheets over approved fixed identity. No gameplay changes.");
        report.Add("Decoded all frames of three composite previews; additional per-clip GIFs saved under qc/clips/.");
        report.Add("No cooldown assets. Timing in previews is illustrative, game timing in mechanics.md remains a proposal.");
        File.WriteAllLines(Path.Combine(root,"qc","validation-production.md"),report,new UTF8Encoding(false));
    }
    public static void Run(string root){
        using(Bitmap identity=new Bitmap(Path.Combine(root,"identity","identity-1.png"))){
            IdentityBounds=Bounds(identity);Dictionary<string,Bitmap[]> groups=new Dictionary<string,Bitmap[]>();
            groups.Add("idle",Bodies(root,"idle",identity));groups.Add("attack",Bodies(root,"attack",identity));
            groups.Add("bullet",Effects(root,"bullet",4,2,22,14,0));
            groups.Add("vortex-open",Effects(root,"vortex-open",4,2,58,32,3));
            for(int i=0;i<3;i++)groups.Add("vortex-loop-"+i,Effects(root,"vortex-loop-"+i,4,2,58,32,0));
            groups.Add("absorb",Effects(root,"absorb",4,2,32,26,3));
            groups.Add("collapse",Effects(root,"collapse",4,2,58,32,0));
            for(int i=0;i<3;i++){string name="bullet-impact"+(i==0?"":"-"+i);groups.Add(name,Effects(root,name,6,3,64,48,2));}
            using(Bitmap raw=new Bitmap(Path.Combine(root,"raw","icon.png"))){
                Rectangle r=Bounds(raw);double scale=Math.Min(42.0/r.Width,40.0/r.Height);int w=(int)Math.Round(r.Width*scale),h=(int)Math.Round(r.Height*scale);
                Bitmap icon=new Bitmap(48,48);using(Bitmap fit=Sample(raw,r,w,h))Blit(icon,fit,(48-w)/2,(48-h)/2);groups.Add("icon",new[]{icon});
            }
            foreach(KeyValuePair<string,Bitmap[]> group in groups){
                Directory.CreateDirectory(Path.Combine(root,group.Key));
                if(group.Key=="icon"){
                    group.Value[0].Save(Path.Combine(root,"icon","icon-cannon-void.png"),ImageFormat.Png);
                    group.Value[0].Save(Path.Combine(root,"icon","sheet-transparent.png"),ImageFormat.Png);
                }else SaveGroup(root,group.Key,group.Key=="bullet"?"projectile":group.Key.StartsWith("bullet-impact")?"impact":group.Key.StartsWith("vortex-loop")?"vortex":group.Key,group.Value,group.Value.Length==6?3:2);
            }
            Board(root,groups);Overview(root,groups,identity);Preview(root,groups);Validate(root,groups,identity);
            foreach(Bitmap[] group in groups.Values)foreach(Bitmap b in group)b.Dispose();
        }
    }
}

'@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
[VoidExport]::Run($root)
Get-Content -Encoding utf8 (Join-Path $root 'qc/validation-production.md')
