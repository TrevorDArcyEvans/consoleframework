namespace Binding.Observables
{
  /// <summary>
  /// Marks the IList or IList&lt;T&gt; with notifications support.
  /// It is not derived from IList and IList&lt;T&gt; to allow
  /// to create both generic and nongeneric implementations.
  /// </summary>
  public interface IObservableList
  {
    event ListChangedHandler ListChanged;
  }

  public delegate void ListChangedHandler(object sender, ListChangedEventArgs args);
}
