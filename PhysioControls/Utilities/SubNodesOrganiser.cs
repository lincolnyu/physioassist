using System;
using System.Windows;
using PhysioControls.ViewModel;

namespace PhysioControls.Utilities
{
    public class SubNodesOrganiser
    {
        #region Constructors

        public SubNodesOrganiser(NodeViewModel root, double initialRadius, double attenuationRate, bool clockwise)

            : this(root, initialRadius, initialRadius, attenuationRate, clockwise)
        {
        }

        public SubNodesOrganiser(NodeViewModel root, double initialRadiusX, double initialRadiusY, 
            double attenuationRate, bool clockwise)
        {
            _root = root;
            _initialRadiusX = initialRadiusX;
            _initialRadiusY = initialRadiusY;
            _attenuationRate = attenuationRate;
            _clockwise = clockwise;
        }

        #endregion

        #region Methods

        public static double GetApproximateInitialRadius(double upperBound, double attenuationRate)
        {
            return (1 - attenuationRate)*upperBound;
        }

        public static double GetApproximateInitialRadius(double upperBound, double attenuationRate, int depth)
        {
            return (1 - attenuationRate)*upperBound/(1 - Math.Pow(attenuationRate, depth));
        }

        public static double GetSmallestRadius(double initialRadius, double attenuationRate, int depth)
        {
            return initialRadius*Math.Pow(attenuationRate, depth - 1);
        }

        public static int GetDepth(BaseNodeViewModel node, bool complete = false)
        {
            if (!complete && !node.IsExpanded || node.SubNodes.Count == 0) return 0;
            int maxDepth = 0;
            foreach (var subNode in node.SubNodes)
            {
                var depth = GetDepth(subNode, complete) + 1;
                if (depth > maxDepth) maxDepth = depth;
            }
            return maxDepth;
        }

        public void Organise(bool complete = false)
        {
            SetRadiusList(complete);
            Organise(_root, 0);
        }

        private void SetRadiusList(bool complete)
        {
            var radiusX = _initialRadiusX;
            var radiusY = _initialRadiusY;
            var depth = GetDepth(_root, complete);
            _radiusXList = new double[depth];
            _radiusYList = new double[depth];
            for (int i = 0; i < depth; radiusX *= _attenuationRate, radiusY *= _attenuationRate, i++)
            {
                _radiusXList[i] = radiusX;
                _radiusYList[i] = radiusY;
            }
        }

        private double GetAngleOfVectorToParent(BaseNodeViewModel node) 
        {
            var subNode = node as SubNodeViewModel;

            if (subNode == null) return 0;  // it's a root node

            var x = subNode.Parent.LocationOnCanvas.X - subNode.LocationOnCanvas.X;
            var y = subNode.Parent.LocationOnCanvas.Y - subNode.LocationOnCanvas.Y;
            var angle = Math.Atan2(y, x);
            return angle;
        }

        private void Organise(BaseNodeViewModel node, int depth) 
        {
            if (depth >= _radiusYList.Length) return;
            var startAngle = GetAngleOfVectorToParent(node);
            SetSubNodeLocations(node, _radiusXList[depth], _radiusYList[depth], startAngle, depth > 0);
            foreach (var subNode in node.SubNodes)
            {
                Organise(subNode, depth + 1);
            }
        }

        private void SetSubNodeLocations(BaseNodeViewModel node, double radiusX, double radiusY, 
            double startingAngle, bool skipStarting = true)
        {
            var numSubNodes = node.SubNodes.Count;
            var angleStep = Math.PI * 2 / numSubNodes;
            if (_clockwise) angleStep = -angleStep;
            var currAngle = startingAngle;
            if (skipStarting) currAngle += angleStep/2;
            for (int i = 0; i < numSubNodes; i++, currAngle += angleStep)
            {
                var subNode = node.SubNodes[i];
                var x = node.LocationOnCanvas.X + radiusX * Math.Cos(currAngle);
                var y = node.LocationOnCanvas.Y + radiusY * Math.Sin(currAngle);
                subNode.LocationOnCanvas = new Point(x, y);
            }
        }

        #endregion

        #region Fields

        private readonly NodeViewModel _root;
        private readonly bool _clockwise;
        private readonly double _initialRadiusX;
        private readonly double _initialRadiusY;
        private readonly double _attenuationRate;
        private double[] _radiusXList;
        private double[] _radiusYList;

        #endregion
    }
}
