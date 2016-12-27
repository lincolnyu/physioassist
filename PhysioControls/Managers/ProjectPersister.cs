using System;
using System.Collections.Generic;
using System.Data.EntityClient;
using System.Linq;
using PhysioControls.EntityDataModel;
using PhysioControls.ViewModel;

namespace PhysioControls.Managers
{
    public class ProjectPersister : DirtChecker, IDisposable
    {
        #region Properties

        public Project Project { get; private set; }

        public ProjectViewModel ProjectViewModel { get; private set; }

        public PhysioProjectContainer ProjectContainer { get; private set; }

        public EntityConnection Connection
        {
            get { return ProjectContainer == null ? null : (EntityConnection)ProjectContainer.Connection; }
        }

        public bool IsConnected
        {
            get { return ProjectContainer != null; }
        }

        #endregion

        #region Constructors
        
        /// <summary>
        ///  Instantitate a persister that loads the project from the specified connection
        /// </summary>
        /// <param name="connection">The database connection the manager is working with</param>
        public ProjectPersister(EntityConnection connection)
        {
            ProjectContainer = new PhysioProjectContainer(connection);

            Project = ProjectContainer.Projects.FirstOrDefault();
            if (Project==null)
            {
                throw new ApplicationException("Failed to load project from the specified connection");
            }

            Project.Persister = this;
            ProjectViewModel = new ProjectViewModel(Project);

            ProcessIds();
        }

        /// <summary>
        ///  Instantiates a persister that uses the specified connection to persist the specified model
        ///  It creates a view model to wrap the model, so view model should be obtained from the property
        /// </summary>
        /// <param name="connection">The database connection the manager is working with</param>
        /// <param name="project">The project to persist</param>
        public ProjectPersister(EntityConnection connection, Project project)
            : this(connection, new ProjectViewModel(project))
        {
        }

        /// <summary>
        ///  Instantiates a persister that uses the specified connection to persist the model contained in the specified view model
        /// </summary>
        /// <param name="connection">The database connection the manager is working with</param>
        /// <param name="projectViewModel">The view model that contains the project to persist</param>
        public ProjectPersister(EntityConnection connection, ProjectViewModel projectViewModel)
            : this(projectViewModel)
        {
            ProjectContainer = new PhysioProjectContainer(connection);
        }

        /// <summary>
        ///  Instantiates a persister that persists the specified model without a connection
        /// </summary>
        /// <param name="project"></param>
        public ProjectPersister(Project project)
            : this(new ProjectViewModel(project))
        {
        }

        /// <summary>
        ///  Instantiates a persister that persists the model in the specified view model without a 
        ///  connection provided for the momeent
        /// </summary>
        /// <param name="projectViewModel">The view model that contains the project to persist</param>
        public ProjectPersister(ProjectViewModel projectViewModel)
        {
            Project = projectViewModel.Model;
            Project.Persister = this;
            ProjectViewModel = projectViewModel;

            _freshModel = true;

            ProcessIds();
        }

        #endregion

        #region Methods

        #region IDisposable Members

        public void Dispose()
        {
            if (ProjectContainer == null) return;
            Flush();
            ProjectContainer.Dispose();
            ProjectContainer = null;
        }

        #endregion

        /// <summary>
        ///  save with a different connection
        /// </summary>
        /// <param name="connection">connection with which to save</param>
        public void SaveAs(EntityConnection connection)
        {
            if (connection ==  null)
            {
                return;
            }

            // TODO I suppose more cases where connection strings are literally equal than are filtered out here
            if (Connection == connection || (Connection != null && Connection.ConnectionString == connection.ConnectionString))
            {
                // the two connections are considered equal
                Save();
                return;
            }

            if (ProjectContainer != null)
            {
                DetachProject();
                _bufferedObjects.Clear();
                ScanProject(o => _bufferedObjects.Add(o));
                ProjectContainer.Dispose();
            }

            ProjectContainer = new PhysioProjectContainer(connection);

            FreshSave();
        }

        private void FreshSave()
        {
            ProjectContainer.CreateDatabase();

            ProjectContainer.Projects.AddObject(Project);
            foreach (var obj in _bufferedObjects)
            {
                ProjectContainer.DataObjects.AddObject(obj);
            }
            _bufferedObjects.Clear();
            ProjectContainer.SaveChanges();
            IsDirty = false;
            _freshModel = false;
        }

        /// <summary>
        ///  saves changes
        /// </summary>
        public void Save()
        {
            if (_freshModel)
            {
                FreshSave();
            }
            else
            {
                ProjectContainer.SaveChanges();
                IsDirty = false;
            }
        }

        private static int GetUnusedId(ICollection<int> usedIds)
        {
            for (var i = 1; ; i++)
            {
                if(usedIds.Contains(i)) 
                    continue;
                usedIds.Add(i);
                return i;
            }
        }

/*
        private static void ReleaseId(ICollection<int> usedIds, int id)
        {
            usedIds.Remove(id);
        }
*/

        /// <summary>
        ///  Detaches subnodes from the given node from the current project container
        /// </summary>
        /// <param name="node">The node whose subnodes are to be detached</param>
        private void DetachSubNodes(BaseNode node)
        {
            var subNodes = node.SubNodes.ToArray();
            var subSubNodes = new SubNode[subNodes.Length][];
            var i = 0;
            foreach (var subNode in subNodes)
            {
                subSubNodes[i++] = subNode.SubNodes.ToArray();
                ProjectContainer.Detach(subNode);
            }

            i = 0;
            foreach (var subNode in subNodes)
            {
                node.SubNodes.Add(subNode);
                subNode.Parent = node;
                foreach (var subSubNode in subSubNodes[i])
                {
                    subNode.SubNodes.Add(subSubNode);
                    subSubNode.Parent = subNode;
                }
                i++;

                DetachSubNodes(subNode);
            }
        }

        /// <summary>
        ///  Detaches the nodes of the specifed page from the current project container
        /// </summary>
        /// <param name="page">The page whose nodes are to be detached</param>
        private void DetachPageNodes(Page page)
        {
            var nodes = page.DataObjects.OfType<Node>().ToArray();
            var subNodes = new SubNode[nodes.Length][];
            var i = 0;
            foreach (var node in nodes)
            {
                subNodes[i++] = node.SubNodes.ToArray();
                ProjectContainer.Detach(node);
            }

            page.DataObjects.Clear();

            i = 0;
            foreach (var node in nodes)
            {
                page.DataObjects.Add(node);
                node.Page = page;
                foreach (var subNode in subNodes[i])
                {
                    node.SubNodes.Add(subNode);
                    subNode.Parent = node;
                }
                i++;

                DetachSubNodes(node);
            }
        }

        /// <summary>
        ///  Detaches the project from the current project container
        /// </summary>
        private void DetachProject()
        {
            var pages = Project.Pages.ToArray();
            ProjectContainer.Detach(Project);

            // detach pages with nodes preserved to add back on later
            var nodes = new Node[pages.Length][];
            for (var i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                nodes[i] = page.DataObjects.OfType<Node>().ToArray();
                ProjectContainer.Detach(page);
            }

            // adds nodes back on to the pages and then detaches the nodes
            for (var i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                Project.Pages.Add(page);
                page.Project = Project;
                foreach (var node in nodes[i])
                {
                    page.DataObjects.Add(node);
                    node.Page = page;
                }

                DetachPageNodes(page);
            }
        }

        private delegate void ModelProcess(DataObject obj);

        private static void ScanNode(BaseNode node, ModelProcess process)
        {
            var q = new Queue<SubNode>();
            while (true)
            {
                foreach (var s in node.SubNodes)
                {
                    q.Enqueue(s);
                }
                if (q.Count == 0) break;
                node = q.Dequeue();

                process(node);
            }
        }

        private static void ScanPage(Page page, ModelProcess process)
        {
            foreach (var dataObject in page.DataObjects)
            {
                process(dataObject);

                var n = dataObject as BaseNode;
                if (n == null) continue;

                ScanNode(n, process);
            }
        }

        private void ScanProject(ModelProcess process)
        {
            _usedIds.Clear();

            foreach (var page in Project.Pages)
            {
                process(page);

                ScanPage(page, process);
            }
        }

        private void ProcessIds()
        {
            var objsToId = new List<DataObject>();
            _usedIds.Clear();
            ScanProject(o=>
                          {
                              if (o.Id != 0) _usedIds.Add(o.Id);
                              else objsToId.Add(o);
                          });
            foreach (var obj in objsToId)
            {
                obj.Id = GetUnusedId(_usedIds);
            }
        }

        private void AddDataObjectNonRecursive(DataObject dataObject)
        {
            // NOTE objects managed by EF cannot be reassigned an ID although we want to always renew ids
            if (dataObject.Id == 0)
            {
                dataObject.Id = GetUnusedId(_usedIds);
            }

            if (ProjectContainer != null)
            {
                if (_bufferedObjectsToRemove.Contains(dataObject))
                {
                    _bufferedObjectsToRemove.Remove(dataObject);
                }
                else
                {
                    ProjectContainer.DataObjects.AddObject(dataObject);
                }
            }
            else
            {
                _bufferedObjects.Add(dataObject);
            }
        }

        private void RemoveObjectNonRecursive(DataObject dataObject)
        {
            // NOTE setting the ID to zero is not allowed
            // NOTE objects detached cannot be manipulated so don't do that
            // NOTE so we can't do anything about the ID

            if (ProjectContainer != null)
            {
                _bufferedObjectsToRemove.Add(dataObject);
            }
            else
            {
                _bufferedObjects.Remove(dataObject);
            }
        }
        
        internal void AddDataObject(DataObject dataObject)
        {
            AddDataObjectNonRecursive(dataObject);

            if (dataObject is Page)
            {
                ScanPage((Page) dataObject, AddDataObjectNonRecursive);
            }
            else if (dataObject is BaseNode)
            {
                ScanNode((BaseNode) dataObject, AddDataObjectNonRecursive);
            }
        }

        internal void RemoveDataObject(DataObject dataObject)
        {
            // clear the ids of objects taken of from the project
            if (dataObject is Page)
            {
                ScanPage((Page) dataObject, RemoveObjectNonRecursive);
            }
            else if (dataObject is BaseNode)
            {
                ScanNode((BaseNode) dataObject, RemoveObjectNonRecursive);
            }

            RemoveObjectNonRecursive(dataObject);
        }

        private void Flush()
        {
            foreach (var obj in _bufferedObjectsToRemove)
            {
                ProjectContainer.DataObjects.DeleteObject(obj);
            }
            ProjectContainer.SaveChanges();
            IsDirty = false;
        }

        #endregion

        #region Fields

        private bool _freshModel;

        private readonly ISet<int> _usedIds = new HashSet<int>();

        /// <summary>
        ///  objects buffered and to be added when the container is online
        /// </summary>
        private readonly ISet<DataObject> _bufferedObjects = new HashSet<DataObject>(); 

        /// <summary>
        ///  objects buffered and to be removed when flushed
        /// </summary>
        private readonly ISet<DataObject> _bufferedObjectsToRemove = new HashSet<DataObject>(); 

        #endregion

    }
}
