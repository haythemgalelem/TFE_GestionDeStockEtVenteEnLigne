﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TFE_GestionDeStockEtVenteEnLigne.Models.Metier
{
    public class Panier
    {
        public int ID { get; set; }
        public String RegisterViewModelID { get; set; }
        public int ProduitID { get; set; }

        public int Quantite { get; set; }
        public Produit Produit { get; set; }
        public Client Client { get; set; }
    }
}
