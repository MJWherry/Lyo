# lyo-automation-cli

Local plans and a remote run. The commands use `lyo-automation-components/plan` and `lyo-automation-api-client`. They do not import React.

`studio` starts `Lyo.Web.Automation.Studio` and opens a local page that renders the plan library and the editor. That page is the only piece that reads the plans directory. `--plan` is forwarded into the studio Play tab.

```text
lyo-automation plans list
lyo-automation plans create Checkout
lyo-automation plans show Checkout
lyo-automation run Checkout.json --api http://localhost:5000
lyo-automation studio --plan plans/Checkout.json
```

`--dir` changes the plans directory (default `./plans`). The automation API does not store plans. `run` posts the file and prints the status when the run settles. Build the package first so the plan page is in `dist/page`.
