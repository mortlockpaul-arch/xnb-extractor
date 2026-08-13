using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnbExtractor.Content;

namespace XnbExtractor.Models;

public class XnbTCSVtoArray
{
    public string[] Strings { get; }
    public int[] Indices { get; }

    public XnbTCSVtoArray(string[] strings, int[] indices)
    {
        Strings = strings;
        Indices = indices;
    }
}