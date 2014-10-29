using FiledRecipes.Domain;
using FiledRecipes.App.Mvp;
using FiledRecipes.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FiledRecipes.Views
{
    /// <summary>
    /// 
    /// </summary>
    public class RecipeView : ViewBase, IRecipeView
    {
        public void Show(IRecipe recipe)
        {
            // Set the header to the recipe name.
            Header = recipe.Name;
            // Displays the header.
            ShowHeaderPanel();

            // Presents the ingredients.
            Console.WriteLine();
            Console.WriteLine("INGREDIENS");
            Console.WriteLine("----------");
            foreach (var ingredient in recipe.Ingredients)
            {
                Console.WriteLine(ingredient);
            }

            // Presents the instructions.
            Console.WriteLine();
            Console.WriteLine("INSTRUKTION");
            Console.WriteLine("-----------");
            foreach (var instruction in recipe.Instructions)
            {
                Console.WriteLine(instruction);
            }

        }
        public void Show(IEnumerable<IRecipe> recipes)
        {
            // Presents the recipes until a key is pressed.
            foreach(var recipe in recipes)
            {
                Show(recipe);
                ContinueOnKeyPressed();
            }
        }
    }
}
