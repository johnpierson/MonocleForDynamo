using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;
using Dynamo.Logging;
using Dynamo.ViewModels;

namespace MonocleViewExtension.RadialMenu
{
    internal class NodeAutocompleteAdapter
    {
        private const string AssemblyName = "NodeAutoCompleteViewExtension";
        private const string ViewModelTypeName =
            "Dynamo.NodeAutoComplete.ViewModels.NodeAutoCompleteBarViewModel";
        private readonly Dispatcher dispatcher;
        private object autocompleteViewModel;
        private Action<string> resultCallback;
        private Action<string> errorCallback;
        private int requestId;

        public NodeAutocompleteAdapter(DynamoViewModel dynamoViewModel, Dispatcher dispatcher)
        {
            DynamoViewModel = dynamoViewModel;
            this.dispatcher = dispatcher;
        }

        private DynamoViewModel DynamoViewModel { get; }

        public void Request(
            PortViewModel portViewModel,
            Action<string> onResult,
            Action<string> onError)
        {
            Cancel();
            var currentRequest = requestId;
            resultCallback = onResult;
            errorCallback = onError;

            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(candidate => candidate.GetName().Name == AssemblyName);
                if (assembly == null)
                {
                    Fail(currentRequest, "Dynamo Node Autocomplete is not loaded.");
                    return;
                }

                var viewModelType = assembly.GetType(ViewModelTypeName, true);
                autocompleteViewModel = Activator.CreateInstance(
                    viewModelType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new object[] { DynamoViewModel },
                    null);
                SetProperty("PortViewModel", portViewModel);

                var requestViewModel = autocompleteViewModel;
                Task.Run(() => FindTopSearchName(currentRequest, requestViewModel));
            }
            catch (Exception exception)
            {
                Log(Unwrap(exception));
                Fail(currentRequest, "Autocomplete could not generate a prediction.");
            }
        }

        public void Cancel()
        {
            requestId++;
            autocompleteViewModel = null;
            resultCallback = null;
            errorCallback = null;
        }

        private void FindTopSearchName(int currentRequest, object requestViewModel)
        {
            try
            {
                var results = Invoke(requestViewModel, "GetSingleAutocompleteResults") as IEnumerable;
                var first = results?.Cast<object>().FirstOrDefault();
                var name = GetProperty(first, "Description") as string;

                dispatcher.BeginInvoke(new Action(() => Publish(currentRequest, name)));
            }
            catch (Exception exception)
            {
                var actualException = Unwrap(exception);
                dispatcher.BeginInvoke(new Action(() =>
                {
                    Log(actualException);
                    Fail(currentRequest, "Autocomplete could not generate a prediction.");
                }));
            }
        }

        private void Publish(int currentRequest, string searchName)
        {
            if (currentRequest != requestId) return;
            if (string.IsNullOrWhiteSpace(searchName))
            {
                Fail(currentRequest, "Autocomplete returned no node name.");
                return;
            }

            var callback = resultCallback;
            autocompleteViewModel = null;
            resultCallback = null;
            errorCallback = null;
            callback?.Invoke(searchName);
        }

        private object GetProperty(object target, string name)
        {
            if (target == null) return null;
            return target.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);
        }

        private void SetProperty(string name, object value)
        {
            autocompleteViewModel.GetType().GetProperty(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(autocompleteViewModel, value);
        }

        private static object Invoke(object target, string name)
        {
            return target.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method => method.Name == name && method.GetParameters().Length == 0)
                .Invoke(target, null);
        }

        private void Fail(int currentRequest, string message)
        {
            if (currentRequest != requestId) return;
            var callback = errorCallback;
            autocompleteViewModel = null;
            resultCallback = null;
            errorCallback = null;
            callback?.Invoke(message);
        }

        private static Exception Unwrap(Exception exception)
        {
            return exception is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException
                : exception;
        }

        private void Log(Exception exception)
        {
            DynamoViewModel?.Model?.Logger?.LogWarning(
                $"Monocle predictive search - {exception.Message}", WarningLevel.Mild);
        }
    }
}
