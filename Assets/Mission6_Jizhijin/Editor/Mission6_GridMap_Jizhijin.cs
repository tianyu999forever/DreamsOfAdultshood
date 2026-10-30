using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Mission6_GridMap_Jizhijin
{
    public const int Margin = 60, Cell = 44;
    public static readonly Color WallColor = new Color32(42, 65, 75, 255);
    public static readonly Color RaisedColor = new Color32(132, 191, 206, 255);
    public static readonly Color RampColor = new Color32(210, 176, 130, 255);
    public static readonly Color FloorColor = new Color32(226, 235, 223, 255);
    static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        {'A',"01110100011000111111100011000110001"}, {'B',"11110100011000111110100011000111110"},
        {'C',"01111100001000010000100001000001111"}, {'D',"11110100011000110001100011000111110"},
        {'E',"11111100001000011110100001000011111"}, {'F',"11111100001000011110100001000010000"},
        {'G',"01111100001000010111100011000101111"}, {'H',"10001100011000111111100011000110001"},
        {'I',"11111001000010000100001000010011111"}, {'J',"00111000100001000010100101001001100"},
        {'K',"10001100101010011000101001001010001"}, {'L',"10000100001000010000100001000011111"},
        {'M',"10001110111010110101100011000110001"}, {'N',"10001110011010110011100011000110001"},
        {'O',"01110100011000110001100011000101110"}, {'P',"11110100011000111110100001000010000"},
        {'Q',"01110100011000110001101011001001101"}, {'R',"11110100011000111110101001001010001"},
        {'S',"01111100001000001110000010000111110"}, {'T',"11111001000010000100001000010000100"},
        {'U',"10001100011000110001100011000101110"}, {'V',"10001100011000110001100010101000100"},
        {'W',"10001100011000110101101011101110001"}, {'X',"10001100010101000100010101000110001"},
        {'Y',"10001100010101000100001000010000100"}, {'Z',"11111000010001000100010001000011111"},
        {'_',"00000000000000000000000000000011111"}, {'0',"01110100011001110101110011000101110"},
        {'1',"00100011000010000100001000010001110"}, {'2',"01110100010000100010001000100011111"},
        {'3',"11110000010000101110000010000111110"}, {'4',"00010001100101010010111110001000010"},
        {'5',"11111100001000011110000010000111110"}, {'6',"01110100001000011110100011000101110"},
        {'7',"11111000010001000100010000100001000"}, {'8',"01110100011000101110100011000101110"},
        {'9',"01110100011000101111000010000101110"}
    };
    static void Rect(Texture2D t, int x, int y, int width, int height, Color color)
    {
        for (int py = Mathf.Max(0, y); py < Mathf.Min(t.height, y + height); py++)
            for (int px = Mathf.Max(0, x); px < Mathf.Min(t.width, x + width); px++) t.SetPixel(px, py, color);
    }
    static void Text(Texture2D t, string text, int x, int y, int scale, Color color)
    {
        foreach (char ch in text)
        {
            if (Glyphs.TryGetValue(ch, out string glyph))
                for (int r = 0; r < 7; r++) for (int c = 0; c < 5; c++)
                    if (glyph[r * 5 + c] == '1') Rect(t, x + c * scale, y + (6 - r) * scale, scale, scale, color);
            x += 6 * scale;
        }
    }
    public static Color TerrainColor(Mission6_StageDefinition_Jizhijin d,int x,int row)
    { if(d.IsWall(x,row))return WallColor;if(d.Height(x,row)>0)return RaisedColor;if(d.Ramp(x,row)!=0)return RampColor;return FloorColor; }
    public static Texture2D Create(Mission6_StageDefinition_Jizhijin d)
    {
        int dim=Margin*2+d.size*Cell;var texture=new Texture2D(dim,dim,TextureFormat.RGB24,false);
        Rect(texture,0,0,dim,dim,new Color32(245,247,240,255));
        for(int row=0;row<d.size;row++)for(int x=0;x<d.size;x++)
        {int px=Margin+x*Cell,py=Margin+row*Cell;Rect(texture,px,py,Cell,Cell,TerrainColor(d,x,row));Rect(texture,px,py,1,Cell,new Color32(160,177,170,255));Rect(texture,px,py,Cell,1,new Color32(160,177,170,255));}
        foreach(var pair in d.crossword)
        {int px=Margin+pair.Key.x*Cell,py=Margin+pair.Key.y*Cell;var c=pair.Value;Rect(texture,px+1,py+1,Cell-1,Cell-1,WallColor);Rect(texture,px+3,py+3,Cell-5,Cell-5,Color.white);Text(texture,c.missing?"_":c.answer.ToString(),px+12,py+5,4,c.missing?new Color32(198,81,25,255):WallColor);if(c.number>0)Text(texture,c.number.ToString(),px+4,py+31,1,WallColor);}
        for(int i=0;i<d.pickups.Length;i++)
        {var c=d.pickups[i];int px=Margin+c.x*Cell,py=Margin+c.y*Cell;Rect(texture,px+3,py+3,38,38,WallColor);Rect(texture,px+5,py+5,34,34,new Color32(249,211,108,255));Text(texture,d.letters[i].ToString(),px+12,py+8,4,WallColor);}
        foreach(var c in d.enemies)
        {int px=Margin+c.x*Cell,py=Margin+c.y*Cell;Rect(texture,px+3,py+3,38,38,new Color32(180,45,45,255));Text(texture,"X",px+10,py+5,5,Color.white);}
        foreach(var c in new[]{d.Start,d.Exit})
        {int px=Margin+c.x*Cell,py=Margin+c.y*Cell;Rect(texture,px+2,py+2,41,41,new Color32(90,171,128,255));Text(texture,c==d.Start?"S":"E",px+10,py+5,5,Color.white);}
        for(int i=0;i<d.size;i++){Text(texture,(i+1).ToString(),Margin+i*Cell+9,34,2,WallColor);Text(texture,(i+1).ToString(),26,Margin+i*Cell+14,2,WallColor);}
        Text(texture,d.size+" X "+d.size,dim/2-80,dim-36,3,WallColor);texture.Apply();
        string path="Assets/Mission6_Jizhijin/Materials/"+(d.number==1?"Mission6_GridMap_Jizhijin.png":"Mission6_GridMap_Level2_Jizhijin.png");
        File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.isReadable=true;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
