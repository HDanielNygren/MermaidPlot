﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Application;
using Contracts;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

while (true)
{
    var path = "";
    var diagram_graph = "";


    int starMenue = ShowMenu.Menu("Solution graph", new[]
    {
                    "Select solution",

                    "Exit"

                });
    if (starMenue == 0)
    {
        Console.Clear();
        Console.WriteLine("Please provide the path to the solution root");
        Console.WriteLine();
        path = Console.ReadLine();

        Console.Clear();
        Console.WriteLine("Please provide the graph to create");
        Console.WriteLine();
        int selectGraph = ShowMenu.Menu("Select graph", new[]
   {
                    "Graph",
                    "Class diagram",

                    "Exit"

                });
        if (selectGraph == 0)
        {
            diagram_graph = "graph";
        }
        if (selectGraph == 1)
        {
            diagram_graph = "class";
        }
        else
        {
        }
    }

    else
    {
        Environment.Exit(0);
    }





    // Create an MSBuild workspace
    if (!MSBuildLocator.IsRegistered)
      MSBuildLocator.RegisterDefaults();

    var workspace = MSBuildWorkspace.Create();

    // Open the solution
    var solution = await workspace.OpenSolutionAsync(path);
    ProjectDependencyGraph graph = solution.GetProjectDependencyGraph();
    // Iterate over all projects in the solution

    var list = new List<ProjectItem>();
    foreach (ProjectId projectId in graph.GetTopologicallySortedProjects())
    {
      var project = solution.Projects.First(p => p.Id == projectId);
      var children = new List<ChildProject>();
      var references = new List<DllReference>();
      project.MetadataReferences.Where(x => x.Display.Contains("Hogia")).ToList().ForEach(x =>
      {
        int index = x.Display.IndexOf("Hogia");
        if (index >= 0)
        {
          string result = x.Display.Substring(index + "Hogia.".Length);
          references.Add(new DllReference { ReferenceName = result });
        }
      });
      project.AllProjectReferences.ToList().ForEach(x =>
      {
        var project_ref = solution.Projects.First(p => p.Id == x.ProjectId);
        children.Add(new ChildProject { ProjectName = project_ref.Name });
      });
      list.Add(new ProjectItem { ProjectName = project.Name, References = references, Children = children });
    }
    string solutionFilePath = solution.FilePath;

    // Get the solution file name with extension
    string solutionFileNameWithExtension = Path.GetFileName(solutionFilePath);

    // Get the solution name without extension
    string solutionName = Path.GetFileNameWithoutExtension(solutionFileNameWithExtension);

    var projectWrapper = new SolutionModel { SolutionName = solutionName, Projects = list };

    var prompt = @$"
    Input: A JSON file representing a C# solution with projects and their references.

    Output: {diagram_graph} visualizing the relationships between the projects in the solution using mermaid diagrams.

    Additional Notes:

    The diagram should use appropriate notation to represent the relationships between projects (e.g., directed arrows for dependencies).
    Consider including project names and other relevant information within the  shapes.
    You are going to use mermaid diagram to generate the output.
    The output should always start with ``` mermaid and end with ```

    For example: ``` mermaid {{Content here}} ``` 
    ";

    string userPrompt = JsonSerializer.Serialize(projectWrapper);
    var result = await new OpenAI().Chat(prompt, userPrompt.Replace(" ", ""));

    File.WriteAllText($@"C:\Users\anton.patron\Desktop\result{diagram_graph}.md", result);
}