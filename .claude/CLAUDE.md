# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run in debug mode
dotnet run

# Build
dotnet build

# Release Build (self-contained, linux-x64)
dotnet publish -c Release \
-r linux-x64 \
--self-contained true \
-p:PublishSingleFile=true \
-p:PublishReadyToRun=true \
-o ./publish

# Restore packages
dotnet restore
```

## NuGet Packages

以下のパッケージが `.csproj` に含まれていること。不足している場合は `dotnet add package <パッケージ名>` で追加する。

| パッケージ | バージョン | 用途 |
|---|---|---|
| `Avalonia` | 11.3.12 | Avalonia UI コアフレームワーク |
| `Avalonia.Desktop` | 11.3.12 | デスクトップ向けサポート |
| `Avalonia.Themes.Fluent` | 11.3.12 | Fluent デザインテーマ |
| `Avalonia.Fonts.Inter` | 11.3.12 | Inter フォント |
| `Avalonia.Diagnostics` | 11.3.12 | デバッグツール（Debug ビルドのみ） |
| `CommunityToolkit.Mvvm` | 8.2.1 | MVVM パターン（`[ObservableProperty]`, `[RelayCommand]` など） |

> **Error hint** — `CS0246: The type or namespace name 'CommunityToolkit' could not be found`: `dotnet add package CommunityToolkit.Mvvm` を実行する。

## Architecture

This is an **Avalonia UI** desktop application using the **MVVM** pattern with **.NET 10**.

### Key patterns

- **ViewLocator** (`ViewLocator.cs`): Automatically resolves Views from ViewModels by replacing `ViewModel` with `View` in the fully-qualified type name. All ViewModels must follow the `MyGuiApp.ViewModels.*ViewModel` → `MyGuiApp.Views.*View` naming convention for auto-resolution to work.

- **ViewModelBase** (`ViewModels/ViewModelBase.cs`): All ViewModels inherit from this, which extends `CommunityToolkit.Mvvm`'s `ObservableObject`. Use `[ObservableProperty]` and `[RelayCommand]` source generators from CommunityToolkit for reactive properties and commands.

- **Compiled bindings**: `AvaloniaUseCompiledBindingsByDefault` is enabled. AXAML views must declare `x:DataType` for compile-time binding validation.

- **Data validation**: Avalonia's built-in `DataAnnotationsValidationPlugin` is disabled in `App.axaml.cs` to avoid conflicts with CommunityToolkit.Mvvm validation. Use CommunityToolkit validation attributes only.

- **Theme**: Fluent theme with `RequestedThemeVariant="Default"` (follows system theme). Change in `App.axaml`.

### Directory layout

- `Views/` — AXAML + code-behind files for UI
- `ViewModels/` — ViewModel classes
- `Models/` — Model classes (empty placeholder folder currently)
- `Assets/` — Static resources (icons, images)

---

## Avalonia UI Development Guidelines (General)

### 1. View Definition Rules (Code-Behind & AXAML)

These rules ensure the connection between the markup and the logic remains intact.

* **Partial Class**: Always use `public partial class` for the View's class definition.
* **x:Class Directive**: The `x:Class` attribute in the `AXAML` file must be the fully qualified name: `[Namespace].[ClassName]`.
  * ❌ `x:Class="MainView"`
  * ✅ `x:Class="YourProject.Views.MainView"`
* **Constructor**: Always call `InitializeComponent();` inside the constructor. This is mandatory for initializing the UI defined in AXAML.

> **Error hint** — `CS0103: The name 'InitializeComponent' does not exist`: Missing `partial` keyword or `x:Class` mismatch.

---

### 2. AXAML 名前空間宣言ルール (xmlns)

* **正しい Avalonia xmlns URL**:
  * ❌ `xmlns="https://github.com/avaloniaui/avalonia"` （末尾に `/avalonia` は不要）
  * ✅ `xmlns="https://github.com/avaloniaui"`
  * この間違いは `InitializeComponent` が見つからない原因になる。

* **XMLの名前空間URLを改行・分割しないこと**:
  * ❌ 改行・分割された例:
    ```xml
    <UserControl xmlns="https://github.com/avaloniaui"
    xmlns:x="http
    ://schemas.microsoft.com/winfx/2006/xaml"
    ```
  * ✅ 正しい例（新規AXAMLファイルは必ずこのテンプレートを使うこと）:
    ```xml
    <UserControl xmlns="https://github.com/avaloniaui"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                 mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
                 x:Class="MyGuiApp.Views.MyView">
    ```

> **Error hint** — `AVLN1001: 'http' is an unexpected token`: 必ず名前空間URLの改行を疑うこと。

---

### 3. Strict File Naming Convention

* **Crucial**: The code-behind file MUST end with `.axaml.cs`, not `.xaml.cs`.
  * ✅ `FooView.axaml` + `FooView.axaml.cs`
  * ❌ `FooView.axaml` + `FooView.xaml.cs` (This causes build errors and AI confusion)
* **Action**: If you see a `.xaml.cs` file, rename it to `.axaml.cs` immediately before editing.

---

### 4. Control Referencing (Fixing CS0103)

Prevents "The name '...' does not exist in the current context" errors.

* **x:Name Directive**: To reference a control from C# code-behind, use **`x:Name="ControlName"`** instead of just `Name="ControlName"`.
* **Direct Access**: If a control has `x:Name="MyControl"` in AXAML, access it directly as `this.MyControl` in the code-behind. Do not use `FindVisualChild` or `VisualTreeHelper`.
* **Source Generators**: Avalonia uses Source Generators to create properties for `x:Name`ed controls. If errors persist, run `dotnet clean` to trigger re-generation.
* **Event Handlers & Namespaces**: When implementing event handlers (e.g., `Click`, `PointerPressed`), you must manually add the required namespaces.
  * ✅ Add **`using Avalonia.Interactivity;`** for `RoutedEventArgs`.
  * ✅ Add **`using Avalonia.Input;`** for `KeyEventArgs` or `PointerEventArgs`.

> **Error hint** — `CS0103: The name 'MyControl' does not exist`: Check for `x:Name="MyControl"` in the AXAML file, ensure `InitializeComponent()` is called, and run `dotnet clean`.

---

### 5. Compiled Bindings & x:DataType

`AvaloniaUseCompiledBindingsByDefault` is enabled project-wide. This means:

* Every View (Window, UserControl) **must** declare `x:DataType` pointing to its ViewModel.
  ```xml
  <UserControl ...
               x:DataType="vm:MyViewModel">
  ```
* Add the ViewModel namespace to the AXAML file:
  ```xml
  xmlns:vm="using:MyGuiApp.ViewModels"
  ```
* Do **not** use `{x:Bind}` — this is a WPF/UWP syntax. Use `{Binding}` in Avalonia.
  * ❌ `Text="{x:Bind MyProperty}"`
  * ✅ `Text="{Binding MyProperty}"`

> **Error hint** — `AVLN0002: Binding path 'Xyz' not found on type '...'`: `x:DataType` が間違っているか、プロパティ名のタイポを確認すること。

---

### 6. MVVM & CommunityToolkit.Mvvm

**This project uses CommunityToolkit.Mvvm exclusively. Do NOT use ReactiveUI.**

#### ObservableProperty の命名規則

`[ObservableProperty]` はフィールド名からプロパティ名を以下のルールで自動生成する。

| フィールド名 | 生成されるプロパティ名 |
|---|---|
| `_myValue` | `MyValue` |
| `myValue` | `MyValue` |
| `m_myValue` | `MyValue` |
| `myXML` | `MyXML` （大文字はそのまま保持） |

* **大文字小文字は厳密に一致させること。**
  * ❌ `MyXml = 0;`　✅ `MyXML = 0;`
* **推奨**: `myXML`、`myHTTPClient` のような途中大文字が混在する名前は避け、`myXml`、`myHttpClient` に統一すると生成名が予測しやすい。
* **確認方法**: 生成されたファイルは `obj/` フォルダ内に出力される。`CS0103` が消えない場合は `obj/Debug/net*/generated/` 以下の `.g.cs` ファイルで実際のプロパティ名を確認する。

> **Error hint** — `CS0103: The name 'Xyz' does not exist`: フィールド定義と生成プロパティ名が一致しているか確認する。

#### RelayCommand (非同期)

* 非同期コマンドは `async Task` を使うこと。`async void` は例外が握りつぶされるため**禁止**。
  ```csharp
  // ✅ 正しい
  [RelayCommand]
  private async Task LoadDataAsync()
  {
      await ...;
  }

  // ❌ 禁止
  [RelayCommand]
  private async void LoadData() { ... }
  ```
* 生成されるコマンド名はメソッド名から `Async` サフィックスを除いたもの + `Command` になる。
  * `LoadDataAsync()` → `LoadDataCommand`

#### Protection Levels (Fixing CS0122)

* Any ViewModel method or property accessed directly by the View must be marked **`public`**.
* Favor `Command` binding over event handlers (like `Button_Click`) to maintain proper MVVM separation.

> **Error hint** — `CS0122: ... is inaccessible due to its protection level`: Change the access modifier to `public` in the ViewModel.

---

### 7. IValueConverter の実装

* Converter クラスは `Converters/` フォルダに配置し、`IValueConverter` を実装する。
  ```csharp
  // Converters/BoolToVisibilityConverter.cs
  namespace MyGuiApp.Converters;

  public class BoolToVisibilityConverter : IValueConverter
  {
      public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
          => value is true;

      public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
          => value is true;
  }
  ```
* AXAML での参照方法:
  ```xml
  xmlns:conv="using:MyGuiApp.Converters"
  ...
  <UserControl.Resources>
      <conv:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
  </UserControl.Resources>
  ...
  <TextBlock IsVisible="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}"/>
  ```

---

### 8. Resources の定義場所

* **アプリ全体で共有するリソース** → `App.axaml` の `<Application.Resources>` に定義する。
* **特定のViewだけで使うリソース** → そのViewの `<UserControl.Resources>` に定義する。
* `App.axaml` のリソースは `{StaticResource}` でどこからでも参照できる。
* リソースファイルを分離する場合は `ResourceDictionary` を使い、`App.axaml` の `MergedDictionaries` に追加する。
  ```xml
  <Application.Resources>
      <ResourceDictionary>
          <ResourceDictionary.MergedDictionaries>
              <ResourceDictionary Source="avares://MyGuiApp/Assets/Styles.axaml"/>
          </ResourceDictionary.MergedDictionaries>
      </ResourceDictionary>
  </Application.Resources>
  ```

---

### 9. Avoid WPF-Specific Types (Avalonia Substitution)

Avalonia is **NOT WPF**. Do not use Windows-only namespaces or types.

| ❌ WPF (禁止) | ✅ Avalonia (代替) |
|---|---|
| `DependencyObject` | `AvaloniaObject` or `Visual` |
| `DependencyProperty` | `AvaloniaProperty` |
| `UIElement` | `Visual` or `Control` |
| `FrameworkElement` | `Control` or `Layoutable` |
| `using System.Windows;` | `using Avalonia;` |
| `using System.Windows.Controls;` | `using Avalonia.Controls;` |
| `{x:Bind ...}` | `{Binding ...}` |
| `Visibility.Collapsed` | `IsVisible="False"` |
| `HorizontalAlignment.Stretch` | `HorizontalAlignment="Stretch"` ※同名だが名前空間に注意 |

---

### 10. Cross-Platform Best Practices

* **Path Handling**: Never hardcode backslashes (`\`). Use `System.IO.Path.Combine()` for all file operations.
* **Font Embedding**: To avoid "tofu" characters (missing glyphs) on Linux, embed font files (.ttf/.otf) as Avalonia Assets and set a global `FontFamily`.
* **Namespace Alignment**: Ensure the C# `namespace` matches the folder structure (e.g., a file in `Views/` should have `namespace ProjectName.Views`).

---

## Troubleshooting

エラーが発生した場合は `/troubleshoot-avalonia` コマンドを使用してください。
