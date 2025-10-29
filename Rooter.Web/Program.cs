using Rooter.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application services
builder.Services.AddScoped<IProjectAssetsParser, ProjectAssetsParser>();
builder.Services.AddScoped<IDependencyAnalyzer, DependencyAnalyzer>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();

// Serve the main page
app.MapGet("/", () => Results.Redirect("/graph.html"));

// Legacy simple page
app.MapGet("/simple", () => Results.Content("""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>NuGet Dependency Analyzer</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .container { max-width: 1200px; margin: 0 auto; }
        .upload-area { border: 2px dashed #ccc; padding: 40px; text-align: center; margin: 20px 0; }
        .upload-area.dragover { border-color: #007acc; background-color: #f0f8ff; }
        button { background-color: #007acc; color: white; padding: 10px 20px; border: none; border-radius: 4px; cursor: pointer; }
        button:hover { background-color: #005a9e; }
        #results { margin-top: 20px; }
        .graph-container { border: 1px solid #ddd; margin: 20px 0; padding: 20px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>NuGet Dependency Analyzer</h1>
        <p>Upload your project.assets.json files to visualize and analyze NuGet package dependencies.</p>
        
        <div class="upload-area" id="uploadArea">
            <p>Drop project.assets.json files here or click to browse</p>
            <input type="file" id="fileInput" multiple accept=".json" style="display: none;">
            <button onclick="document.getElementById('fileInput').click()">Browse Files</button>
        </div>
        
        <div>
            <button onclick="loadExample()">Load Example</button>
        </div>
        
        <div id="results"></div>
    </div>

    <script>
        const uploadArea = document.getElementById('uploadArea');
        const fileInput = document.getElementById('fileInput');
        const results = document.getElementById('results');

        uploadArea.addEventListener('dragover', (e) => {
            e.preventDefault();
            uploadArea.classList.add('dragover');
        });

        uploadArea.addEventListener('dragleave', () => {
            uploadArea.classList.remove('dragover');
        });

        uploadArea.addEventListener('drop', (e) => {
            e.preventDefault();
            uploadArea.classList.remove('dragover');
            const files = e.dataTransfer.files;
            handleFiles(files);
        });

        fileInput.addEventListener('change', (e) => {
            handleFiles(e.target.files);
        });

        async function handleFiles(files) {
            const projectsData = [];
            
            for (let file of files) {
                if (file.name.endsWith('.json')) {
                    const content = await file.text();
                    projectsData.push({
                        projectName: file.name.replace('.json', ''),
                        projectAssetsJson: content
                    });
                }
            }
            
            if (projectsData.length > 0) {
                await analyzeProjects(projectsData);
            }
        }

        async function analyzeProjects(projectsData) {
            try {
                results.innerHTML = '<p>Analyzing dependencies...</p>';
                
                let response;
                if (projectsData.length === 1) {
                    response = await fetch('/api/dependency/analyze', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(projectsData[0])
                    });
                } else {
                    response = await fetch('/api/dependency/analyze-multiple', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ projects: projectsData })
                    });
                }
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                
                const graph = await response.json();
                displayResults(graph);
                
            } catch (error) {
                results.innerHTML = `<p style="color: red;">Error: ${error.message}</p>`;
            }
        }

        async function loadExample() {
            try {
                results.innerHTML = '<p>Loading example...</p>';
                
                const response = await fetch('/api/dependency/example');
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                
                const graph = await response.json();
                displayResults(graph);
                
            } catch (error) {
                results.innerHTML = `<p style="color: red;">Error: ${error.message}</p>`;
            }
        }

        function displayResults(graph) {
            const nodeCount = Object.keys(graph.nodes).length;
            const edgeCount = graph.edges.length;
            
            let html = `
                <div class="graph-container">
                    <h2>Dependency Analysis Results</h2>
                    <p><strong>Project:</strong> ${graph.projectName || 'Unknown'}</p>
                    <p><strong>Target Framework:</strong> ${graph.targetFramework || 'Unknown'}</p>
                    <p><strong>Total Packages:</strong> ${nodeCount}</p>
                    <p><strong>Total Dependencies:</strong> ${edgeCount}</p>
                    
                    <h3>Package List</h3>
                    <ul>
            `;
            
            const sortedNodes = Object.values(graph.nodes).sort((a, b) => a.name.localeCompare(b.name));
            for (const node of sortedNodes) {
                const directText = node.isDirectDependency ? ' (Direct)' : '';
                html += `<li>${node.name} v${node.version}${directText}</li>`;
            }
            
            html += `
                    </ul>
                    
                    <h3>Dependency Tree (JSON)</h3>
                    <pre style="background: #f5f5f5; padding: 10px; overflow-x: auto;">${JSON.stringify(graph, null, 2)}</pre>
                </div>
            `;
            
            results.innerHTML = html;
        }
    </script>
</body>
</html>
""", "text/html"));

app.Run();
