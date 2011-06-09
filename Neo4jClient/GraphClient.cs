﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using RestSharp;

namespace Neo4jClient
{
    public class GraphClient : IGraphClient
    {
        readonly RestClient client;
        internal RootEndpoints RootEndpoints;

        public GraphClient(Uri rootUri)
            : this(rootUri, new Http())
        {
        }

        public GraphClient(Uri rootUri, IHttpFactory httpFactory)
        {
            client = new RestClient(rootUri.AbsoluteUri) { HttpFactory = httpFactory };
        }

        public void Connect()
        {
            var request = new RestRequest("", Method.GET);
            var response = client.Execute<RootEndpoints>(request);

            if (response.ResponseStatus != ResponseStatus.Completed)
                throw new ApplicationException("Failed to connect to server.", response.ErrorException);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new ApplicationException(string.Format(
                    "Received a non-200 HTTP response when connecting to the server. The response status was: {0} {1}",
                    (int)response.StatusCode,
                    response.StatusDescription));

            RootEndpoints = response.Data;
            RootEndpoints.Node = RootEndpoints.Node.Substring(client.BaseUrl.Length);
            RootEndpoints.NodeIndex = RootEndpoints.NodeIndex.Substring(client.BaseUrl.Length);
            RootEndpoints.RelationshipIndex = RootEndpoints.RelationshipIndex.Substring(client.BaseUrl.Length);
            RootEndpoints.ReferenceNode = RootEndpoints.ReferenceNode.Substring(client.BaseUrl.Length);
            RootEndpoints.ExtensionsInfo = RootEndpoints.ExtensionsInfo.Substring(client.BaseUrl.Length);
        }

        public NodeReference Create<TNode>(TNode node, params IRelationship<TNode>[] relationships) where TNode : class
        {
            if (node == null)
                throw new ArgumentNullException("node");

            if (RootEndpoints == null)
                throw new InvalidOperationException("The graph client is not connected to the server. Call the Connect method first.");

            if (relationships.Any())
                throw new NotImplementedException("Relationships aren't implemented yet.");

            var request = new RestRequest(RootEndpoints.Node, Method.POST) {RequestFormat = DataFormat.Json};
            request.AddBody(node);

            var response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.Created)
                throw new ApplicationException(string.Format(
                    "Received an unexpected HTTP status when executing the request. The response status was: {0} {1}",
                    (int)response.StatusCode,
                    response.StatusDescription));

            var nodeLocation = response.Headers.GetParameter("Location");
            var nodeId = int.Parse(GetLastPathSegment(nodeLocation));

            return new NodeReference(nodeId);
        }

        public TNode Get<TNode>(NodeReference reference)
        {
            if (RootEndpoints == null)
                throw new InvalidOperationException("The graph client is not connected to the server. Call the Connect method first.");

            var nodeResouce = RootEndpoints.Node + "/" + reference.Id;
            var request = new RestRequest(nodeResouce, Method.GET);
            var response = client.Execute<NodePacket<TNode>>(request);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return default(TNode);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new ApplicationException(string.Format(
                    "Received an unexpected HTTP status when executing the request. The response status was: {0} {1}",
                    (int)response.StatusCode,
                    response.StatusDescription));

            return response.Data.Data;
        }

        public RelationshipReference CreateRelationship<TRelationshipType>(NodeReference sourceNode, NodeReference targetNode) where TRelationshipType : IRelationshipType, new()
        {
            return CreateRelationshipInternal(sourceNode, targetNode, new TRelationshipType(), null);
        }

        public RelationshipReference CreateRelationship<TRelationshipType, TData>(NodeReference sourceNode, NodeReference targetNode, TData data) where TRelationshipType : IRelationshipType<TData>, new()
        {
            return CreateRelationshipInternal(sourceNode, targetNode, new TRelationshipType(), data);
        }

        RelationshipReference CreateRelationshipInternal(NodeReference sourceNode, NodeReference targetNode, IRelationshipType relationshipType, object data)
        {
            if (sourceNode == null) throw new ArgumentNullException("sourceNode");
            if (targetNode == null) throw new ArgumentNullException("targetNode");

            if (RootEndpoints == null)
                throw new InvalidOperationException("The graph client is not connected to the server. Call the Connect method first.");

            throw new NotImplementedException();
        }

        static string GetLastPathSegment(string uri)
        {
            var path = new Uri(uri).AbsolutePath;
            return path
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .LastOrDefault();
        }
    }
}