using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FiledRecipes.Domain
{
    /// <summary>
    /// Holder for recipes.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        /// <summary>
        /// Represents the recipe section.
        /// </summary>
        private const string SectionRecipe = "[Recept]";

        /// <summary>
        /// Represents the ingredients section.
        /// </summary>
        private const string SectionIngredients = "[Ingredienser]";

        /// <summary>
        /// Represents the instructions section.
        /// </summary>
        private const string SectionInstructions = "[Instruktioner]";

        /// <summary>
        /// Occurs after changes to the underlying collection of recipes.
        /// </summary>
        public event EventHandler RecipesChangedEvent;

        /// <summary>
        /// Specifies how the next line read from the file will be interpreted.
        /// </summary>
        private enum RecipeReadStatus { Indefinite, New, Ingredient, Instruction };

        /// <summary>
        /// Collection of recipes.
        /// </summary>
        private List<IRecipe> _recipes;

        /// <summary>
        /// The fully qualified path and name of the file with recipes.
        /// </summary>
        private string _path;

        /// <summary>
        /// Indicates whether the collection of recipes has been modified since it was last saved.
        /// </summary>
        public bool IsModified { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the RecipeRepository class.
        /// </summary>
        /// <param name="path">The path and name of the file with recipes.</param>
        public RecipeRepository(string path)
        {
            // Throws an exception if the path is invalid.
            _path = Path.GetFullPath(path);

            _recipes = new List<IRecipe>();
        }

        /// <summary>
        /// Returns a collection of recipes.
        /// </summary>
        /// <returns>A IEnumerable&lt;Recipe&gt; containing all the recipes.</returns>
        public virtual IEnumerable<IRecipe> GetAll()
        {
            // Deep copy the objects to avoid privacy leaks.
            return _recipes.Select(r => (IRecipe)r.Clone());
        }

        /// <summary>
        /// Returns a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to get.</param>
        /// <returns>The recipe at the specified index.</returns>
        public virtual IRecipe GetAt(int index)
        {
            // Deep copy the object to avoid privacy leak.
            return (IRecipe)_recipes[index].Clone();
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="recipe">The recipe to delete. The value can be null.</param>
        public virtual void Delete(IRecipe recipe)
        {
            // If it's a copy of a recipe...
            if (!_recipes.Contains(recipe))
            {
                // ...try to find the original!
                recipe = _recipes.Find(r => r.Equals(recipe));
            }
            _recipes.Remove(recipe);
            IsModified = true;
            OnRecipesChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Deletes a recipe.
        /// </summary>
        /// <param name="index">The zero-based index of the recipe to delete.</param>
        public virtual void Delete(int index)
        {
            Delete(_recipes[index]);
        }

        /// <summary>
        /// Raises the RecipesChanged event.
        /// </summary>
        /// <param name="e">The EventArgs that contains the event data.</param>
        protected virtual void OnRecipesChanged(EventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler handler = RecipesChangedEvent;

            // Event will be null if there are no subscribers. 
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(this, e);
            }
        }

        // Load method.
        // Using "virtual" since it should be able to be overridden in a derived class.
        public virtual void Load()
        {
            RecipeReadStatus status = RecipeReadStatus.Indefinite;
            Recipe localRecipe = null;

            // 1. Creates a list.
            List<IRecipe> recipes = new List<IRecipe>();

            // 2. Creates a reader that can read a textfile.
            // the "using" statement makes sure that unmanaged resources are disposed correctly.
            using (StreamReader reader = new StreamReader("App_Data/Recipes.txt"))
            {
                // 3. Reads the file, one line at a time.
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    switch (line)
                    {
                        // If the readed line is the same as SectionRecipe, change status.
                        case SectionRecipe:
                            status = RecipeReadStatus.New; // Status is a new recipe.
                            break;

                        // If the readed line is the same as SectionIngredients, change status.
                        case (SectionIngredients):
                            status = RecipeReadStatus.Ingredient; // Status is the ingredient section.
                            break;

                        // If the readed line is the same as SectionInstructions, change status.
                        case (SectionInstructions):
                            status = RecipeReadStatus.Instruction; // Status is the instruction section.
                            break;

                        // If it's none of the above...
                        default:
                            // Check which status is currently active.
                            switch (status)
                            {
                                    // If it's a new recipe, create a new object from the Recipeclass with the recipes name (line).
                                case RecipeReadStatus.New:
                                    localRecipe = new Recipe(line);
                                    recipes.Add(localRecipe);
                                    break;

                                    // It it's a ingredient, split the line into 3 sections, the ";" devides the sections.
                                case RecipeReadStatus.Ingredient:
                                    string[] splitIngredient = line.Split(new String[] { ";" }, StringSplitOptions.None);

                                    // If it's more than 3 sections, something is wrong.
                                    if (splitIngredient.Length != 3)
                                    {
                                        throw new FileFormatException();
                                    }

                                    // Create a ingredient object with the 3 sections, amount, measure and name.
                                    Ingredient ingredient = new Ingredient();
                                    ingredient.Amount = splitIngredient[0];
                                    ingredient.Measure = splitIngredient[1];
                                    ingredient.Name = splitIngredient[2];
                                    localRecipe.Add(ingredient);
                                    break;

                                    // If it's an instruction, just add the read line.
                                case RecipeReadStatus.Instruction:
                                    localRecipe.Add(line);
                                    break;

                                    // If it's none of the above, something is wrong.
                                case RecipeReadStatus.Indefinite:
                                    throw new FileFormatException();

                            }
                            break;
                    }
                }
            }
            // 4-5 Sorts the list alphabetaclly.
            _recipes = recipes.OrderBy(recipe => recipe.Name).ToList();

            // 6. Indicates that the list with recipes are unchanged.
            IsModified = false;

            // 7. Event that tells the recipe has been read.
            OnRecipesChanged(EventArgs.Empty);
        }


        // Save method.
        // This also uses "virtual" since it should be able to be overridden in a derived class.
        public virtual void Save()
        {
            // 1. Opens the textfile.
            using (StreamWriter writer = new StreamWriter("App_Data/Recipe.txt"))
            {

                // 2. Loop for all recipes.
                foreach (var recipe in _recipes)
                {
                    // The recipes names.
                    writer.WriteLine(SectionRecipe);
                    writer.WriteLine(recipe.Name);

                    // The ingredients, also uses a "foreach" since there are more than one ingredient in each recipe.
                    writer.WriteLine(SectionIngredients);
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        writer.WriteLine("{0};{1};{2}", ingredient.Amount, ingredient.Measure, ingredient.Name);
                    }

                    // The instructions. This also uses a "foreach" since there are more than one line of instructions.
                    writer.WriteLine(SectionInstructions);
                    foreach (var instruction in recipe.Instructions)
                    {
                        writer.WriteLine(instruction);

                    }
                }
            }
        }
    }
}
