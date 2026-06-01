# Next Steps
## Court terme


	1. XXX pas forcement --- Migrer les loggers de `DatabaseService` vers `DomainService`
2. Revoir le comportement du scroll sur le header Eyes
	3. XXX Revoir la gestion du `IsDirty` sur la Combobox du `DataVm`
4. A FAIRE !!!!!! Pièce similaire sur un contact — ne pas afficher la part dans la CB, ou déclencher une alerte
5. Contact similaire — afficher une indication visuelle
	6. XXX Contrainte d'unicité sur le nom d'un requirement --- fait aussi sur part et modeldata
7. **[Engine]** Intégrer les tolérances de requirement dans le moteur de résultats
	8. XXX Afficher les requirements désactivés (`IsActive = false`) en grisé sur la `ResultPage`
9. Trier les modèles par date de modification, le plus récent sélectionné par défaut
	10. XXX Rafraîchir les compteurs parts / requirements à l'ouverture et à la suppression d'un modèle
	11. Interdire les noms vides pour une part créée via le TreeView
12. Évaluer l'ajout de VM pour les panels (coût/bénéfice probablement défavorable)
	13. Safeguard pour la création de data si `PartId` n'existe pas
	14. Binding `PageModel` sur le modèle actif
	15. ComboBox de requirement qui ne garde pas le binding
	16. Erreur lors du calcul car le binding `PartId` 1/2 des exigences n'est pas mis à jour directement
	17. Sauvegarde qui incrémente le naming à chaque fois → ne pas prendre en compte son propre nom DB
	18. Erreur lors du close `PartDBPage` ?? Laquelle
19. Voir la regle : Utiliser le constructeur principal (IDE0290) et les regle Messages d'erreur de compilation
	

## Moyen terme

	- Introduire un service d'instanciation centralisé pour les services et VM, afin de décharger le `MainVM` (DI) pas forcement neccesaire surachitecture
- Refonte de la page d'accueil (modèles récents, version, utilisateur, tâches utilisateur, about, documentation, etc.)
- Logo animé
- Gestion des erreurs de base de données (fallback)
- Mise en place du Ctrl+Z (session complète — chantier majeur)
- Mettre en place le pattern de validation name, error etc... (behaviors trigger)

## Long terme

- Intégration des éléments métiers
- Liaisons cinématiques
- Tolérances de réglages
- Multi-tolérances
- Indicateur visuel de valeur incohérente

# Ideas

- Pop-up personnalisé pour l'état pré-calcul, en remplacement des MessageBox standards
- Suppression via le ruban : ouvrir le TreeView flottant pour permettre la multi-sélection et la suppression groupée — à étudier si un panneau dédié est déjà affiché (data ou req)