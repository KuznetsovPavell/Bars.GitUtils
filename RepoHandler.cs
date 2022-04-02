using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Text.RegularExpressions;

// Функционал работы с репозиторием.

namespace Bars.GitUtils
{
#nullable disable
    public class RepoHandler : IDisposable
    {
        private readonly Repository _repository;
        private readonly string _repoUser;
        private readonly string _repoPass;

        // User information to create a merge commit        
        private readonly Signature signature = new(new Identity("MERGE_USER_NAME", "MERGE_USER_EMAIL"), DateTimeOffset.Now);
        public Branch CurrentBranch { private set; get; }
        public RepoHandler(string repoPath, string repoUser, string repoPass)
        {
            _repository = new Repository(repoPath);
            _repoUser = repoUser;
            _repoPass = repoPass;
        }

        // ---=== Method ===--- //
        public void CheckoutBranch(string branch)
        {
            CurrentBranch = Commands.Checkout(_repository, _repository.Branches[branch]);
        }

        // ---=== Method ===--- //
        public void PullBranch()
        {
            // Credential information to fetch
            PullOptions options = new()
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = new CredentialsHandler(
                (url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials()
                    {
                        Username = _repoUser,
                        Password = _repoPass
                    })
                }
            };
            // Pull
            try
            {
                Commands.Pull(_repository, signature, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server is not available\n\n");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("----------Bars.GitUtils.RepoHandler.cs.PullBranch()----------");
            }
        }

        // ---=== Method ===--- //
        public MatchCollection DiffBetweenTagsSQL(string tagFrom, string tagTo)
        {
            Tree tagto = _repository.Lookup<Commit>(tagTo).Tree;      //tagTo
            Tree tagfrom = _repository.Lookup<Commit>(tagFrom).Tree;  //tagFrom

            var changesFiles = _repository.Diff.Compare<Patch>(tagfrom, tagto, new List<string>() { @"sql\install.sql" })[@"sql\install.sql"];
            MatchCollection outValue = Regex.Matches(changesFiles.Patch, @"^\+@..*", RegexOptions.Multiline);
            return outValue;
        }

        // ---=== Method ===--- //
        public List<Tuple<string, string, ChangeKind>> DiffBetweenTagsWEB(string tagOld, string tagNew)
        {
            Tree tagnew = _repository.Lookup<Commit>(tagNew).Tree;  //tagTo
            Tree tagold = _repository.Lookup<Commit>(tagOld).Tree;  //tagFrom
            var changesFiles = _repository.Diff.Compare<Patch>(tagold, tagnew, new List<string>() { "*web*\\*" });

            var listOut = new List<Tuple<string, string, ChangeKind>>();

            foreach (var item in changesFiles)
            {
                listOut.Add(Tuple.Create(item.Path.Replace('/', Path.DirectorySeparatorChar).ToLower()
                                   , item.OldPath.Replace('/', Path.DirectorySeparatorChar).ToLower()
                                   , item.Status));
            }
            return listOut;
        }

        // ---=== Method ===--- //
        public List<Tuple<string, string, ChangeKind>> DiffBetweenTagsPSQL(string tagFrom, string tagTo)
        {
            Tree tagto = _repository.Lookup<Commit>(tagTo).Tree;      //tagTo
            Tree tagfrom = _repository.Lookup<Commit>(tagFrom).Tree;  //tagFrom

            var changesFiles = _repository.Diff.Compare<Patch>(tagfrom, tagto, new List<string>() { "*" });
            //            MatchCollection outValue = Regex.Matches(changesFiles.Patch, @"^\+@..*", RegexOptions.Multiline);
            var listOut = new List<Tuple<string, string, ChangeKind>>();

            foreach (var item in changesFiles)
            {
                listOut.Add(Tuple.Create(item.Path.Replace('/', Path.DirectorySeparatorChar).ToLower()
                                   , item.OldPath.Replace('/', Path.DirectorySeparatorChar).ToLower()
                                   , item.Status));
            }
            return listOut;
        }

        // ---=== Method ===--- //
        // Add
        public void AddFileToRepo(string fileToAdd)
        {
            _repository.Index.Add(fileToAdd);
            _repository.Index.Write();
        }

        // ---=== Method ===--- //
        // Commit
        public void CommitRepoChanges(string message)
        {
            _repository.Commit(message, signature, signature);
        }

        // Erase Object Repository
        private bool disposed = false;

        // ---=== Method ===--- //
        protected virtual void Dispose(bool disposing)
        {
            disposed = !disposed || disposed;
        }

        public void Dispose() // Implement IDisposable
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RepoHandler() // the finalizer
        {
            Dispose(false);
        }

        // ---=== Method ===--- //
        internal static string MakeColumn(string valJson, int width)
        {
            if (valJson == null) valJson = "";
            if (valJson.Length > width)
                valJson = valJson[..width];
            return valJson.PadRight(width);
        }

        static void Main(string[] args)
        {
        }
    }

}
