﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Fetcher.Model.Scraper
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public Uri Url { get; set; }
    }
}
