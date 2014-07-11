using System;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Database.Server;

namespace Libber
{
    public class Db
    {
        private static readonly Lazy<IDocumentStore> TheDocStore = new Lazy<IDocumentStore>(() =>
        {
            NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(8181);
            var docStore = new EmbeddableDocumentStore {
                DataDirectory = "~\\Db",
            #if DEBUG
                UseEmbeddedHttpServer = true,
                Configuration = { Port = 8181 }
            #endif
            };
            docStore.Initialize();
            return docStore;
        });

        public static IDocumentStore DocumentStore { get { return TheDocStore.Value; } }
    }
}
