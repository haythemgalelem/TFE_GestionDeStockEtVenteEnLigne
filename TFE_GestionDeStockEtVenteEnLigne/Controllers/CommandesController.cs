using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TFE_GestionDeStockEtVenteEnLigne.Data;
using TFE_GestionDeStockEtVenteEnLigne.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TFE_GestionDeStockEtVenteEnLigne.Models.Adaptateur;

namespace TFE_GestionDeStockEtVenteEnLigne.Controllers
{
    [Authorize(Roles = "gestionnaire,client")]
    public class CommandesController : Controller
    {
        private readonly TFEContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommandesController(UserManager<ApplicationUser> userManager, TFEContext context)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Commandes
        public  IActionResult Index()
        {
            var IdUser = _userManager.GetUserId(User);
            var IdClient = _context.Clients.Where(c => c.RegisterViewModelID == IdUser).ToArray();
            var listeCommande =  _context.Commandes.Where(c=>c.ClientID== IdClient[0].ID);

            return View(listeCommande);
        }

        // GET: Commandes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //var commande = await _context.Commandes.Include(p=>p.Possede).ThenInclude(pr=>pr.produits)
            //    .SingleOrDefaultAsync(m => m.ID == id);
            Commande commande = await _context.Commandes.Include(c => c.Possede).SingleOrDefaultAsync(m => m.ID == id);
            Produit produit;
            if (commande == null)
            {
                return NotFound();
            }
            foreach (var i in commande.Possede)
            {
                 produit = await _context.Produits
                    .Include(p => p.Categorie)
                        .ThenInclude(c => c.CategorieParent)
                    .Include(p => p.MotClef)
                        .ThenInclude(mp => mp.MotClef)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(m => m.ID == i.ProduitID);
                i.Produit = produit;
            }
            

            return View(commande);
        }

        // GET: Commandes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Commandes/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create( CommandeAdresseAdaptateur Adaptateur)
        {
            if (ModelState.IsValid)
            {
                //ajout de l'adresse dans la bd
                _context.Add(Adaptateur.Adresse);
                await _context.SaveChangesAsync();
                //r�cup des infoi utilisateur et du panier
                var IdUser = _userManager.GetUserId(User);
                var IdClient = _context.Clients.Where(c => c.RegisterViewModelID == IdUser);
                var tFEContext = _context.Panier.Include(p => p.Produit).Where(p => p.RegisterViewModelID == IdUser).ToArray();
                var Client = IdClient.ToArray();
                //cr�ation de la comande
                Commande commande = new Commande();
                commande.AdresseID = Adaptateur.Adresse.ID;
                commande.ClientID = Client[0].ID;
                commande.DateCommade = DateTime.Now;
                if (User.IsInRole("gestionnaire"))
                    commande.EnCours = false;
                else
                    commande.EnCours = true;
                _context.Add(commande);
                await _context.SaveChangesAsync();
                //ajout des produit a la commande
                int idCommande = commande.ID;
                foreach (var e in tFEContext)
                {
                    Possede p = new Possede();
                    p.CommandeID = idCommande;
                    p.ProduitID = e.ProduitID;
                    e.Produit.QuantiteStock = e.Produit.QuantiteStock -e.Quantite;
                    p.Quantite = e.Quantite;
                    _context.Add(p);
                }
                await _context.SaveChangesAsync();
                //creation de la facture
                Facture f = new Facture();
                f.CommandeID = idCommande;
                _context.Add(f);
                await _context.SaveChangesAsync();
                //vidage du panier
                var panier = _context.Panier.Where(p => p.RegisterViewModelID == IdUser);
                _context.Panier.RemoveRange(panier);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index","Commandes");
        }

        // GET: Commandes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var commande = await _context.Commandes.SingleOrDefaultAsync(m => m.ID == id);
            if (commande == null)
            {
                return NotFound();
            }
            return View(commande);
        }

        // POST: Commandes/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,dateCommade,EnCours,Envoie")] Commande commande)
        {
            if (id != commande.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(commande);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommandeExists(commande.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            return View(commande);
        }

        // GET: Commandes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var commande = await _context.Commandes
                .SingleOrDefaultAsync(m => m.ID == id);
            if (commande == null)
            {
                return NotFound();
            }

            return View(commande);
        }

        // POST: Commandes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var commande = await _context.Commandes.SingleOrDefaultAsync(m => m.ID == id);
            _context.Commandes.Remove(commande);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool CommandeExists(int id)
        {
            return _context.Commandes.Any(e => e.ID == id);
        }
    }
}
