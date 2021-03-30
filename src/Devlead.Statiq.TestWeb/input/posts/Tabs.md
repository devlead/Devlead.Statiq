## Static Tab ShortCode test

<?# TabGroup?>
<?*
tabs:
  - name: Intro
    content: |
      ## Famous words

      Quote some latin

      > Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec id elementum neque, ac pharetra tortor. Vivamus ac vehicula augue. Curabitur pretium commodo nisi id pellentesque.
      
      ## Useful?

      I think so!
      
  - content: |
      > Below code is an example of an TabGroup tab fetching into code fence
    code: ../../../Devlead.Statiq/Devlead.Statiq.csproj
  - code: ../../settings.yml
  - code: ../../Devlead.Statiq.TestWeb.csproj
?>
<?#/ TabGroup?>

## Some more tabs

<?# TabGroup?>
<?*
tabs:
  - name: TOC
    content: |
      ## Hello

      Some text.

      ### World

      Some more text.

  - name: Table
    content: |
      | Tables   |      Are      |  Cool |
      |----------|:-------------:|------:|
      | col 1 is |  left-aligned | $1600 |
      | col 2 is |    centered   |   $12 |
      | col 3 is | right-aligned |    $1 |

  - name: List
    content: |
      - Lorem
      - Ipsum
      - Dolor

  - name: Graph
    content: |
      ```mermaid
      flowchart LR
          subgraph azureprod [Azure fa:fa-cloud]
              AppProduction(App Service fa:fa-globe) --> api-production[APIM fa:fa-sitemap<br>api-production]
              AppProduction --> vnetprd[vnet-prd]
          end
          subgraph onpremprod [On-premises fa:fa-home]
              vnetprd ----> SQLProd[SQL Prod fa:fa-server ] --> DBProd[(DBProd)]
          end
      ```

?>
<?#/ TabGroup?>