using System;
using XnbExtractor.Models;

namespace XnbExtractor
{
    public class XnbTEdgePalManager
    {
        private enum enPalPattern
        {
            p00ColKey,
            p01PantsA0,
            p02PantsA1,
            p03PantsB0,
            p04PantsB1,
            p05PantsC0,
            p06PantsC1,
            p07Shirt0,
            p08Shirt1,
            p09Shirt2,
            p10Shoes0,
            p11Shoes1,
            p12None,
            p13None,
            p14None,
            p15None,
            p16Black,
            p17Eye0,
            p18Eye1,
            p19Eye2,
            p20Skin0,
            p21Skin1,
            p22Skin2,
            p23Skin3,
            p24Hair0,
            p25Hair1,
            p26Hair2,
            p27Etc0,
            p28Etc1,
            p29Etc2,
            p30None,
            p31None
        }

        protected const int LINENUM = 32;

        protected const int PAL_MAX = 256;

        protected readonly int PALNUM;

        protected readonly int COLNUM;

        public uint[] PalARGB { get; private set; }

        public XnbTEdgePalManager(params XnbTEdgePal[] tPals)
        {
            PALNUM = tPals.Length;
            COLNUM = PALNUM * 32;
            PalARGB = new uint[256 * PALNUM];
            for (int i = 0; i < PALNUM; i++)
            {
                Array.Copy(tPals[i].PalARGB, 0, PalARGB, i * 256, 256);
            }
        }

        private int GetColLine(int cNo)
        {
            return cNo * 32;
        }

        public uint GetPal_Black(int cNo)
        {
            return PalARGB[GetColLine(cNo) + 16];
        }

        public uint[] GetPal_Eye(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[3]
            {
            PalARGB[colLine + 17],
            PalARGB[colLine + 18],
            PalARGB[colLine + 19]
            };
        }

        public uint[] GetPal_Skin(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[4]
            {
            PalARGB[colLine + 20],
            PalARGB[colLine + 21],
            PalARGB[colLine + 22],
            PalARGB[colLine + 23]
            };
        }

        public uint[] GetPal_Hair(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[3]
            {
            PalARGB[colLine + 24],
            PalARGB[colLine + 25],
            PalARGB[colLine + 26]
            };
        }

        public uint[] GetPal_Etc(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[3]
            {
            PalARGB[colLine + 27],
            PalARGB[colLine + 28],
            PalARGB[colLine + 29]
            };
        }

        public uint[] GetPal_PantsA(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[2]
            {
            PalARGB[colLine + 1],
            PalARGB[colLine + 2]
            };
        }

        public uint[] GetPal_PantsB(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[2]
            {
            PalARGB[colLine + 3],
            PalARGB[colLine + 4]
            };
        }

        public uint[] GetPal_PantsC(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[2]
            {
            PalARGB[colLine + 5],
            PalARGB[colLine + 6]
            };
        }

        public uint[] GetPal_Shirt(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[3]
            {
            PalARGB[colLine + 7],
            PalARGB[colLine + 8],
            PalARGB[colLine + 9]
            };
        }

        public uint[] GetPal_Shoes(int cNo)
        {
            int colLine = GetColLine(cNo);
            return new uint[2]
            {
            PalARGB[colLine + 10],
            PalARGB[colLine + 11]
            };
        }

        public uint[] GetPalAll(int cEye, int cSkin, int cHair, int cEtc, int cPantsA, int cPantsB, int cPantsC, int cShirt, int cShoes)
        {
            uint[] pal_Eye = GetPal_Eye(cEye);
            uint[] pal_Skin = GetPal_Skin(cSkin);
            uint[] pal_Hair = GetPal_Hair(cHair);
            uint[] pal_Etc = GetPal_Etc(cEtc);
            uint[] pal_PantsA = GetPal_PantsA(cPantsA);
            uint[] pal_PantsB = GetPal_PantsB(cPantsB);
            uint[] pal_PantsC = GetPal_PantsC(cPantsC);
            uint[] pal_Shirt = GetPal_Shirt(cShirt);
            uint[] pal_Shirt2 = GetPal_Shirt(cShoes);
            return new uint[32]
            {
            0u,
            pal_PantsA[0],
            pal_PantsA[1],
            pal_PantsB[0],
            pal_PantsB[1],
            pal_PantsC[0],
            pal_PantsC[1],
            pal_Shirt[0],
            pal_Shirt[1],
            pal_Shirt[2],
            pal_Shirt2[0],
            pal_Shirt2[1],
            0u,
            0u,
            0u,
            0u,
            GetPal_Black(0),
            pal_Eye[0],
            pal_Eye[1],
            pal_Eye[2],
            pal_Skin[0],
            pal_Skin[1],
            pal_Skin[2],
            pal_Skin[3],
            pal_Hair[0],
            pal_Hair[1],
            pal_Hair[2],
            pal_Etc[0],
            pal_Etc[1],
            pal_Etc[2],
            0u,
            0u
            };
        }

        //public uint[] GetPalAll(TColoring col)
        //{
        //    return GetPalAll(col.cEye, col.cSkin, col.cHair, col.cEtc, col.cPantsA, col.cPantsB, col.cPantsC, col.cShirt, col.cShoes);
        //}
    }

}