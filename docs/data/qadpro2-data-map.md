# KST v2 Data Map

Generated from `DataMap.xlsx`.

## Metadata

- Source file: `DataMap.xlsx`
- Generated UTC: `2026-08-06T23:23:17+00:00`
- Tables: `26` (25 from `DataMap.xlsx` + `sct_det`, added Stage 8D.5 — see that table's entry)
- Fields: `561` (555 from `DataMap.xlsx` + 6 `sct_det` fields)
- Validated fields: `558`

## Agent Usage Rules

1. Do not invent table names or field names.
2. Prefer fields marked **Validated = Yes**.
3. If a join is not documented elsewhere, ask before guessing.
4. Avoid `SELECT *`; select only the fields needed by the feature.
5. Treat this as schema/business metadata only, not live transactional data.

## Database: QADPRO2

### Table: `ad_mstr`

- **Business name:** Address Master
- **Source sheet:** `QADPRO2`
- **Fields:** 24

| Field | Description | Validated |
|---|---|---:|
| `ad_addr` | Address Code | Yes |
| `ad_attn` | Attention | Yes |
| `ad_attn2` | Attention 2 | Yes |
| `ad_bus_relation` | Business Relation Code | Yes |
| `ad_city` | City | Yes |
| `ad_country` | Country | Yes |
| `ad_county` | County | Yes |
| `ad_ctry` | Country Code | Yes |
| `ad_domain` | Domain | Yes |
| `ad_email` | E-mail | Yes |
| `ad_email2` | E-mail 2 | Yes |
| `ad_ext` | Extension | Yes |
| `ad_ext2` | Extension | Yes |
| `ad_lang` | Language | Yes |
| `ad_line1` | Address Line 1 | Yes |
| `ad_line2` | Address Line 2 | Yes |
| `ad_line3` | Address Line 3 | Yes |
| `ad_name` | Company/Customer Name | Yes |
| `ad_phone` | Phone Number | Yes |
| `ad_phone2` | Phone Number 2 | Yes |
| `ad_sort` | Company/Customer Name | Yes |
| `ad_state` | State | Yes |
| `ad_type` | Company, Customer, Enduser, Ship-To, slsprsn, or Supplier | Yes |
| `ad_zip` | Zip Code | Yes |

### Table: `cmt_det`

- **Business name:** Transaction Comments
- **Source sheet:** `QADPRO2`
- **Fields:** 19

| Field | Description | Validated |
|---|---|---:|
| `cmt_cmmt##1` | Comment 1 | Yes |
| `cmt_cmmt##10` | Comment 10 | Yes |
| `cmt_cmmt##11` | Comment 11 | Yes |
| `cmt_cmmt##12` | Comment 12 | Yes |
| `cmt_cmmt##13` | Comment 13 | Yes |
| `cmt_cmmt##14` | Comment 14 | Yes |
| `cmt_cmmt##15` | Comment 15 | Yes |
| `cmt_cmmt##2` | Comment 2 | Yes |
| `cmt_cmmt##3` | Comment 3 | Yes |
| `cmt_cmmt##4` | Comment 4 | Yes |
| `cmt_cmmt##5` | Comment 5 | Yes |
| `cmt_cmmt##6` | Comment 6 | Yes |
| `cmt_cmmt##7` | Comment 7 | Yes |
| `cmt_cmmt##8` | Comment 8 | Yes |
| `cmt_cmmt##9` | Comment 9 | Yes |
| `cmt_domain` | Domain | Yes |
| `cmt_indx` | Comment Index | Yes |
| `cmt_ref` | Comment Reference | Yes |
| `cmt_seq` | Comment Sequence/Page Number | Yes |

### Table: `in_mstr`

- **Business name:** Inventory Master
- **Source sheet:** `QADPRO2`
- **Fields:** 17

| Field | Description | Validated |
|---|---|---:|
| `in_avg_iss` | Average Issues | Yes |
| `in_cnt_date` | Last Cycle Count Date | Yes |
| `in_domain` | Domain | Yes |
| `in_loc` | Location | Yes |
| `in_mrp` | MRP Required (T/F) | Yes |
| `in_part` | Part Number | Yes |
| `in_proj_use` | Projected Use | Yes |
| `in_qty_all` | Total Allocated | Yes |
| `in_qty_avail` | Quantity Available to Use | Yes |
| `in_qty_nonet` | Total Quantity NonNet | Yes |
| `in_qty_oh` | Total Quantity On Hand | Yes |
| `in_qty_ord` | Total Quantity On Order | Yes |
| `in_qty_req` | Total Quantity Required for Demand | Yes |
| `in_rec_date` | Last Receive Date | Yes |
| `in_sfty_stk` | Safety Stock | Yes |
| `in_site` | Site | Yes |
| `in_supp_consign_qty` | Supplier Consigned Quantity | Yes |

### Table: `is_mstr`

- **Business name:** Inventory Status Master
- **Source sheet:** `QADPRO2`
- **Fields:** 6

| Field | Description | Validated |
|---|---|---:|
| `is_avail` | Available (T/F) | Yes |
| `is_desc` | Status Code Description | Yes |
| `is_domain` | Domain | Yes |
| `is_frozen` | Frozen (T/F) | Yes |
| `is_nettable` | Nettable (T/F) | Yes |
| `is_status` | Inventory Status Code | Yes |

### Table: `kmfg_mstr`

- **Business name:** Manufacturer Master
- **Source sheet:** `QADPRO2`
- **Fields:** 6

| Field | Description | Validated |
|---|---|---:|
| `kmfg_id` | QAD Manufacturer ID | Yes |
| `kmfg_name` | Manufacturer Name | Yes |
| `kmfg_reach` | REACH Level | Yes |
| `kmfg_reach_exp` | REACH Expiration Date | Yes |
| `kmfg_status` | Status | Yes |
| `kmfg_type` | Manufacturer Type | Yes |

### Table: `kmfg_part`

- **Business name:** Manufacturer Parts
- **Source sheet:** `QADPRO2`
- **Fields:** 8

| Field | Description | Validated |
|---|---|---:|
| `kmp_apr` | APR | Yes |
| `kmp_apr_cmmt` | APR Comment | Yes |
| `kmp_mfg_id` | QAD Manufacturer ID | Yes |
| `kmp_mfg_part` | Manufacturer Part Number | Yes |
| `kmp_rohs_cmmt` | RoHS Comment | Yes |
| `kmp_rohs_date` | RoHS Date | Yes |
| `kmp_status` | Part Status | Yes |
| `kmp_um` | Unit of Measure | Yes |

### Table: `ktlot_mstr`

- **Business name:** Supplimental Lot Information
- **Source sheet:** `QADPRO2`
- **Fields:** 16

| Field | Description | Validated |
|---|---|---:|
| `ktlot_auditor` | Auditor | Yes |
| `ktlot_chr01` | Category Code | Yes |
| `ktlot_chr03` | Manufacturer Part Number | Yes |
| `ktlot_chr04` | CMS Bin Location | Yes |
| `ktlot_container` | Container Type | Yes |
| `ktlot_container_id` | Container ID | Yes |
| `ktlot_coo` | Country of Origin | Yes |
| `ktlot_loc_from` | Receipt Location | Yes |
| `ktlot_loc_to` | Dist Location | Yes |
| `ktlot_log01` | Bonded Material (T/F) | Yes |
| `ktlot_nbr` | Lot Number | Yes |
| `ktlot_num_lab` | Number Label | Yes |
| `ktlot_origin` | Lot Origin | Yes |
| `ktlot_part` | Lot Part Number | Yes |
| `ktlot_part_rev` | Lot Part Revision | Yes |
| `ktlot_prod_type` | Product Type | Yes |

### Table: `ld_det`

- **Business name:** Location Detail
- **Source sheet:** `QADPRO2`
- **Fields:** 14

| Field | Description | Validated |
|---|---|---:|
| `ld_cmtindx` | Comment Index Number | Yes |
| `ld_cnt_date` | Last Cycle Count Date | Yes |
| `ld_cust_consign_qty` | Customer Consigned Quantity | Yes |
| `ld_date` | Received Date | Yes |
| `ld_domain` | Domain | Yes |
| `ld_expire` | Expiration Date | Yes |
| `ld_loc` | Location | Yes |
| `ld_lot` | Lot/Serial | Yes |
| `ld_part` | Item Number | Yes |
| `ld_qty_oh` | Location Quantity On-Hand | Yes |
| `ld_ref` | Reference | Yes |
| `ld_site` | Site | Yes |
| `ld_status` | Inventory Status | Yes |
| `ld_supp_consign_qty` | Supplier Consigned Quantity | Yes |

### Table: `lot_mstr`

- **Business name:** Lot Master
- **Source sheet:** `QADPRO2`
- **Fields:** 15

| Field | Description | Validated |
|---|---|---:|
| `lot__chr01` | Manufactured Lot | Yes |
| `lot__chr05` | Receiver Number | Yes |
| `lot__chr06` | Site | Yes |
| `lot__dec01` | Lot Quantity | Yes |
| `lot__dte01` | Manufacture Date | Yes |
| `lot__dte02` | Expiration Date | Yes |
| `lot__dte03` | Receipt Date | Yes |
| `lot__dte04` | FIFO Date | Yes |
| `lot_cmtindx` | Lot Comment Index Number | Yes |
| `lot_domain` | Domain | Yes |
| `lot_line` | Line/ID | Yes |
| `lot_nbr` | Lot PO Number or KSS Designation | Yes |
| `lot_part` | Part Number | Yes |
| `lot_serial` | Lot Serial Number | Yes |
| `lot_rev` | Lot Revision | Yes |

### Table: `mrp_det`

- **Business name:** Material Requirements Detail
- **Source sheet:** `QADPRO2`
- **Fields:** 12

| Field | Description | Validated |
|---|---|---:|
| `mrp_dataset` | fcs_sum, pod_det, rps_mstr, sod_det, wod_det, wo_mstr, wo_scrap | Yes |
| `mrp_detail` | Work Order Component, Planned Order, Purchase Order, FORECAST, Repetitive Component, Repetitive Schedule, Sales Order, Scrap Requirement, Purchase Order, Work Order, Work Order Component | Yes |
| `mrp_domain` | Domain | Yes |
| `mrp_due_date` | Due Date | Yes |
| `mrp_line` | WOID | Yes |
| `mrp_nbr` | WO# | Yes |
| `mrp_ord_site` | MRP Site | No |
| `mrp_part` | Item Number | Yes |
| `mrp_qty` | Quantity | Yes |
| `mrp_rel_date` | Release Date | Yes |
| `mrp_site` | Production Site (AR, KS, KV, SW, CH, VT, VM, NW) | Yes |
| `mrp_type` | SUPPLY, SUPPLYF, SUPPLYP, DEMAND | Yes |

### Table: `pi_mstr`

- **Business name:** Price List Master
- **Source sheet:** `QADPRO2`
- **Fields:** 16

| Field | Description | Validated |
|---|---|---:|
| `pi__chr01` | Quote Identifier | Yes |
| `pi__dec01` | Quoted Volume | Yes |
| `pi_cmtindx` | Comment Index Number | Yes |
| `pi_curr` | Currency | Yes |
| `pi_desc` | Price List Description | Yes |
| `pi_domain` | Domain | Yes |
| `pi_expire` | Pricing Expiration Date | Yes |
| `pi_list` | Price List Name | Yes |
| `pi_list_id` | QAD Price List ID Number | Yes |
| `pi_manual` | Manual Prices (T/F) | Yes |
| `pi_max_ord` | Maximum Orders | Yes |
| `pi_max_qty` | Maximum Quantity | Yes |
| `pi_min_net` | Minimum Order | Yes |
| `pi_part_code` | Part Number | Yes |
| `pi_start` | Pricing Start Date | Yes |
| `pi_um` | Unit of Measure | Yes |

### Table: `pid_det`

- **Business name:** Price List Detail
- **Source sheet:** `QADPRO2`
- **Fields:** 4

| Field | Description | Validated |
|---|---|---:|
| `pid_amt` | Price | Yes |
| `pid_domain` | Domain | Yes |
| `pid_list_id` | QAD Price List ID Number | Yes |
| `pid_qty` | Minimum Order Quantity for Price | Yes |

### Table: `pl_mstr`

- **Business name:** Product Line Master
- **Source sheet:** `QADPRO2`
- **Fields:** 8

| Field | Description | Validated |
|---|---|---:|
| `pl__chr01` | Customer Name | Yes |
| `pl__chr02` | Program Name | Yes |
| `pl__chr03` | Product Line Code (IOS) | Yes |
| `pl__chr04` | Product Line Status Code | Yes |
| `pl_desc` | Product Line Description | Yes |
| `pl_domain` | Domain | Yes |
| `pl_prod_line` | Product Line | Yes |
| `pl_sls_acct` | Sales Account | Yes |

### Table: `po_mstr`

- **Business name:** Purchase Order Master
- **Source sheet:** `QADPRO2`
- **Fields:** 26

| Field | Description | Validated |
|---|---|---:|
| `po__chr01` | Credit Terms Comment | Yes |
| `po_buyer` | Buyer Initials | Yes |
| `po_cmtindx` | Purchase Order Comment Index Number | Yes |
| `po_confirm` | PO Confirmed (T/F) | Yes |
| `po_consignment` | Consignment (T/F) | Yes |
| `po_cr_terms` | Credit Terms | Yes |
| `po_curr` | Currency | Yes |
| `po_disc_pct` | Discount Percentage | Yes |
| `po_domain` | Domain | Yes |
| `po_due_date` | Due Date | Yes |
| `po_ex_rate` | Exchange Rate | Yes |
| `po_fob` | FOB Shipping Point | Yes |
| `po_nbr` | Purchase Order Number | Yes |
| `po_ord_date` | Order Date | Yes |
| `po_print` | Unprinted (T/F) | Yes |
| `po_rev` | Order Revision | Yes |
| `po_rev_date` | Revision Date | Yes |
| `po_rmks` | Remarks | Yes |
| `po_sched` | Supplier Scheduled (T/F)  (KSS) | Yes |
| `po_ship` | Ship To Code | Yes |
| `po_shipvia` | Ship Via | Yes |
| `po_site` | Site | Yes |
| `po_stat` | PO Status | Yes |
| `po_type` | Purchase Order Type | Yes |
| `po_user_id` | Entered By | Yes |
| `po_vend` | Supplier Code | Yes |

### Table: `pod_det`

- **Business name:** Purchase Order Detail
- **Source sheet:** `QADPRO2`
- **Fields:** 32

| Field | Description | Validated |
|---|---|---:|
| `pod__chr06` | Tracking Number | Yes |
| `pod__chr09` | PER/Air Authorization Number | Yes |
| `pod__chr10` | Proforma Invoice Number | Yes |
| `pod__dte01` | Ship Date | Yes |
| `pod__dte02` | Line Added Date | Yes |
| `pod__log01` | Confirmed (T/F) | Yes |
| `pod_consignment` | Consignment (T/F) | Yes |
| `pod_desc` | Description | Yes |
| `pod_domain` | Domain | No |
| `pod_due_date` | Due Date | Yes |
| `pod_line` | Line | Yes |
| `pod_loc` | PO Location | Yes |
| `pod_nbr` | Purchase Order Number | Yes |
| `pod_part` | Item Number | Yes |
| `pod_po_site` | Site | Yes |
| `pod_pr_list` | Price List | Yes |
| `pod_project` | Project Code | Yes |
| `pod_pur_cost` | PO Price | Yes |
| `pod_qty_ord` | Quantity Ordered | Yes |
| `pod_qty_rcvd` | Quantity Received | Yes |
| `pod_rev` | Revision | Yes |
| `pod_sched` | KSS/Feed | Yes |
| `pod_site` | Site | Yes |
| `pod_so_status` | Sales Order Status | Yes |
| `pod_sod_line` | Line Number | Yes |
| `pod_status` | Line Status | Yes |
| `pod_std_cost` | Standard Cost | Yes |
| `pod_type` | PO Type | Yes |
| `pod_um` | Unit of Measure | Yes |
| `pod_um_conv` | UM Conversion | Yes |
| `pod_vpart` | Supplier Part Number | Yes |
| `pod_wo_lot` | Work Order ID | Yes |

### Table: `ps_mstr`

- **Business name:** Product Structure Master
- **Source sheet:** `QADPRO2`
- **Fields:** 15

| Field | Description | Validated |
|---|---|---:|
| `ps_cmtindx` | Comment Index Number | Yes |
| `ps_comp` | Component Number | Yes |
| `ps_comp_um` | Unit of Measure | Yes |
| `ps_domain` | Domain | Yes |
| `ps_end` | null or >=today | Yes |
| `ps_exclusive` | ps_comp exclusive to ps_par (T/F) | Yes |
| `ps_mandatory` | Mandatory (T/F) | Yes |
| `ps_mod_date` | Last Modified Date | Yes |
| `ps_op` | Operation Level | Yes |
| `ps_par` | First level below parent item.  Main Subassembly | Yes |
| `ps_qty_per` | Quantity Per | Yes |
| `ps_ref` | Reference Number | Yes |
| `ps_rmks` | Remarks | Yes |
| `ps_scrp_pct` | Scrap Percentage | Yes |
| `ps_start` | As Of Date (null or <=today) | Yes |

### Table: `pt_mstr`

- **Business name:** Part Master
- **Source sheet:** `QADPRO2`
- **Fields:** 60

| Field | Description | Validated |
|---|---|---:|
| `pt_abc` | Part ABC Class | Yes |
| `pt_added` | Added Date | Yes |
| `pt_article` | Article Number | Yes |
| `pt_atp_family` | ATP Family | Yes |
| `pt_atp_horizon` | ATP Horizon | Yes |
| `pt_auto_lot` | Auto Lot | Yes |
| `pt_avg_int` | Average Interval | Yes |
| `pt_bom_code` | BOM Code | Yes |
| `pt_break_cat` | Part Price Break Category | Yes |
| `pt_buyer` | Buyer/Planner | Yes |
| `pt_comm_code` | Commodity Code | Yes |
| `pt_critical` | Key Item | Yes |
| `pt_cum_lead` | Cumulative Lead Time | Yes |
| `pt_desc1` | Item Description 1 | Yes |
| `pt_desc2` | Item Description 2 | Yes |
| `pt_domain` | Domain | Yes |
| `pt_draw` | Part Drawing | Yes |
| `pt_drwg_loc` | Part Drawing Location | Yes |
| `pt_drwg_size` | Part Drawing Size | Yes |
| `pt_dsgn_grp` | Part Design Group | Yes |
| `pt_group` | Part Group | Yes |
| `pt_insp_lead` | Inspection Lead Time | Yes |
| `pt_insp_rqd` | Inspection Required (T/F) | Yes |
| `pt_loc` | Location | Yes |
| `pt_loc_type` | Location Type | Yes |
| `pt_memo_type` | Memo Order Type | Yes |
| `pt_mfg_lead` | Manufacturing Lead Time | Yes |
| `pt_mod_date` | Modified Date | Yes |
| `pt_mrp` | MRP (T/F) | Yes |
| `pt_ord_max` | Maximum Order | Yes |
| `pt_ord_min` | Minimum Order | Yes |
| `pt_ord_mult` | Order Multiple | Yes |
| `pt_ord_pol` | Order Policy | Yes |
| `pt_part` | Item Number | Yes |
| `pt_part_type` | Part Type | Yes |
| `pt_phantom` | Phantom (T/F) | Yes |
| `pt_pm_code` | PM Code | Yes |
| `pt_po_site` | PO Site | Yes |
| `pt_prod_line` | Product Line | Yes |
| `pt_pur_lead` | Purchasing Lead Time | Yes |
| `pt_rev` | Revision | Yes |
| `pt_sfty_stk` | Safety Stock Quantity | Yes |
| `pt_sfty_time` | Safety Time | Yes |
| `pt_shelflife` | Shelf Life | Yes |
| `pt_ship_wt` | Ship Weight | Yes |
| `pt_ship_wt_um` | Ship Weight UM | Yes |
| `pt_site` | Site | Yes |
| `pt_size` | Size | Yes |
| `pt_size_um` | Size Unit of Measure | Yes |
| `pt_status` | Part Status Code | Yes |
| `pt_taxable` | Taxable | Yes |
| `pt_taxc` | Tax Class | Yes |
| `pt_transtype` |  | No |
| `pt_um` | Unit of Measure | Yes |
| `pt_user1` | ROHS | Yes |
| `pt_user2` | User Field 2 | Yes |
| `pt_userid` | User ID | Yes |
| `pt_vend` | Vendor/Supplier | Yes |
| `pt_warr_cd` | IOS | Yes |
| `pt_yield_pct` | Yield Percentage | Yes |

### Table: `ptp_det`

- **Business name:** Part Detail
- **Source sheet:** `QADPRO2`
- **Fields:** 30

| Field | Description | Validated |
|---|---|---:|
| `ptp_bom_code` | BOM Code | Yes |
| `ptp_buyer` | Buyer Number (Purchased Parts)/Planner Code (Manufactured Parts) | Yes |
| `ptp_cum_lead` | Cumulative Lead Time | Yes |
| `ptp_domain` | Domain | Yes |
| `ptp_draw` | Part Drawing | Yes |
| `ptp_ins_lead` | Inspection Lead Time | Yes |
| `ptp_ins_rqd` | Inspection Required (T/F) | Yes |
| `ptp_iss_pol` | Issue Policy | Yes |
| `ptp_mfg_lead` | Manufacturing Lead Time | Yes |
| `ptp_mod_date` | Modified Date | Yes |
| `ptp_ord_max` | Maximum Order | Yes |
| `ptp_ord_min` | Minimum Order | Yes |
| `ptp_ord_mult` | Order Multiple | Yes |
| `ptp_ord_per` | Order Period | Yes |
| `ptp_ord_pol` | Order Policy | Yes |
| `ptp_ord_qty` | Order Quantity | Yes |
| `ptp_part` | Item Number | Yes |
| `ptp_phantom` | Phantom (T/F) | Yes |
| `ptp_plan_ord` | Planned Order (T/F) | Yes |
| `ptp_pm_code` | PM Code (P - Purchased, M - Manufactured) | Yes |
| `ptp_po_site` | PO Site | Yes |
| `ptp_pur_lead` | Purchasing Lead Time | Yes |
| `ptp_rev` | Revision | Yes |
| `ptp_routing` | Routing Code | Yes |
| `ptp_sfty_stk` | Safety Stock | Yes |
| `ptp_sfty_tme` | Safety Time | Yes |
| `ptp_site` | Site | Yes |
| `ptp_timefnce` | Timefence | Yes |
| `ptp_vend` | Supplier | Yes |
| `ptp_yld_pct` | Yield Percentage | Yes |

### Table: `sct_det`

- **Business name:** Standard Cost Detail
- **Source sheet:** _not in `DataMap.xlsx`_ — added Stage 8D.5 (Component Info Backend), confirmed
  directly against live QADPRO2 schema/data rather than the original workbook.
- **Fields:** 6

| Field | Description | Validated |
|---|---|---:|
| `sct_domain` | Domain | Yes |
| `sct_site` | Site | Yes |
| `sct_part` | Part Number | Yes |
| `sct_sim` | Simulation Name (accepted Stage 8D.5 filter: `'Standard'`, case-insensitive match, for the Component Detail Standard Cost field — other simulations such as Current/KPI/PurCst exist and must not be selected) | Yes |
| `sct_cst_date` | Cost Effective Date (selection tie-break: latest date wins) | Yes |
| `sct_cst_tot` | Total Standard Cost | Yes |

### Table: `so_mstr`

- **Business name:** Sales Order Master
- **Source sheet:** `QADPRO2`
- **Fields:** 41

| Field | Description | Validated |
|---|---|---:|
| `so__chr01` | Shipping Account | Yes |
| `so__chr03` | C/S Hold (T/F) | Yes |
| `so__chr05` | No-Charge Account | Yes |
| `so__chr06` | Shipping Comment | Yes |
| `so_bill` | Sales Order Bill To | Yes |
| `so_bol` | Sales Order Bill Of Lading | Yes |
| `so_channel` | Sales Order Channel (NRE, Standard, ADJ, RMA-IW, RMA-OOW) | Yes |
| `so_cmtindx` | Comment Index Number | Yes |
| `so_conf_date` | Confirmed Date | Yes |
| `so_consignment` | Consignment (T/F) | Yes |
| `so_cr_terms` | Credit Terms | Yes |
| `so_curr` | Currency | Yes |
| `so_cust` | Customer Number | Yes |
| `so_cust_po` | Customer Purchase Order Number | Yes |
| `so_domain` | Domain | Yes |
| `so_due_date` | Due Date | Yes |
| `so_fob` | FOB | Yes |
| `so_fr_terms` | Sales Order Freight Terms | Yes |
| `so_lang` | Language | Yes |
| `so_nbr` | Sales Order Number | Yes |
| `so_ord_date` | Order Date | Yes |
| `so_po` | Customer PO Number | Yes |
| `so_pricing_dt` | Pricing Date | Yes |
| `so_quote` | Quote Number | Yes |
| `so_req_date` | Requested Date | Yes |
| `so_rev` | Sales Order Revision | Yes |
| `so_reviewed` | Reviewed (T/F) | Yes |
| `so_rmks` | Remarks | Yes |
| `so_sched` | Scheduled (T/F) | Yes |
| `so_ship` | Ship To | Yes |
| `so_ship_date` | Date Shipped | Yes |
| `so_shipvia` | Ship Via | Yes |
| `so_site` | Sites | Yes |
| `so_slspsn##1` | Salesperson 1 | Yes |
| `so_slspsn##2` | Salesperson 2 | Yes |
| `so_slspsn##3` | Salesperson 3 | Yes |
| `so_slspsn##4` | Salesperson 4 | Yes |
| `so_stat` | Action Status | Yes |
| `so_trl1_amt` | Trailer 1 Amount | Yes |
| `so_type` | Sales Order Type | Yes |
| `so_userid` | Sales Order Entered By | Yes |

### Table: `sod_det`

- **Business name:** Sales Order Detail
- **Source sheet:** `QADPRO2`
- **Fields:** 55

| Field | Description | Validated |
|---|---|---:|
| `sod__chr03` | C/S Line Hold (T/F) | Yes |
| `sod__chr05` | Revision | Yes |
| `sod__chr06` | QA Hold (T/F) | Yes |
| `sod_cmtindx` | Comment Index | Yes |
| `sod_comment##1` | Line Comment 1 | Yes |
| `sod_comment##2` | Line Comment 2 | Yes |
| `sod_comment##3` | Line Comment 3 | Yes |
| `sod_comment##4` | Line Comment 4 | Yes |
| `sod_comment##5` | Line Comment 5 | Yes |
| `sod_compl_date` | Complete Date | Yes |
| `sod_compl_stat` | Complete Status | Yes |
| `sod_confirm` | Confirmed (T/F) | Yes |
| `sod_consignment` | Consignment (T/F) | Yes |
| `sod_custpart` | Customer Part Number | Yes |
| `sod_custref` | Customer Reference | Yes |
| `sod_desc` | Description | Yes |
| `sod_disc_pct` | Discount Percentage | Yes |
| `sod_dock` | Dock Date | Yes |
| `sod_domain` | Domain | Yes |
| `sod_due_date` | Due Date | Yes |
| `sod_hold_stat` | Hold Status | Yes |
| `sod_intrans_loc` | Transit Location | Yes |
| `sod_inv_nbr` | Invoice Number | Yes |
| `sod_line` | Line Number | Yes |
| `sod_list_pr` | List Price | Yes |
| `sod_loc` | SO Location | Yes |
| `sod_mod_date` | Modified Date | Yes |
| `sod_modelyr` | Model Year | Yes |
| `sod_nbr` | Sales Order number | Yes |
| `sod_part` | Part Number | Yes |
| `sod_partial` | Partial Shipments Ok | Yes |
| `sod_per_date` | Perform Date | Yes |
| `sod_price` | PO Price | Yes |
| `sod_pricing_dt` | Pricing Date | Yes |
| `sod_prodline` | Product Line | Yes |
| `sod_project` | Project Code | Yes |
| `sod_promise_date` | Promise Date | Yes |
| `sod_qty_ivcd` | Quantity Invoiced | Yes |
| `sod_qty_ord` | Quantity Ordered | Yes |
| `sod_qty_pend` | Quantity Pending | Yes |
| `sod_qty_pick` | Quantity Picked | Yes |
| `sod_qty_ship` | Quantity Shipped | Yes |
| `sod_req_date` | Required Date | Yes |
| `sod_sched` | Scheduled (T/F) | Yes |
| `sod_shipvia` | Ship Via | Yes |
| `sod_site` | Site | Yes |
| `sod_slspsn##1` | Salesperson #1 - Scheduler | Yes |
| `sod_slspsn##2` | Salesperson #2 | Yes |
| `sod_slspsn##3` | Salesperson #3 | Yes |
| `sod_slspsn##4` | Salesperson #4 - Accounting Tech | Yes |
| `sod_status` | Status | Yes |
| `sod_std_cost` | Standard Cost | Yes |
| `sod_trade_sale_po` | Trade Sales PO | Yes |
| `sod_type` | Ship Type | Yes |
| `sod_um` | Unit of Measure | Yes |

### Table: `tr_hist`

- **Business name:** Inventory Transaction History
- **Source sheet:** `QADPRO2`
- **Fields:** 39

| Field | Description | Validated |
|---|---|---:|
| `tr_addr` | Shipment Address | Yes |
| `tr_assay` | Assay Percentage | Yes |
| `tr_batch` | Batch | Yes |
| `tr_begin_qoh` | Beginning Total On Hand Balance | Yes |
| `tr_date` | Transaction date | Yes |
| `tr_domain` | Transaction Domain | Yes |
| `tr_expire` | Expire Date | Yes |
| `tr_line` | Transaction Line | Yes |
| `tr_loc` | Transaction location | Yes |
| `tr_loc_begin` | Beginning Location Balance | Yes |
| `tr_lot` | Work Order ID Number | Yes |
| `tr_nbr` | WO Number | Yes |
| `tr_part` | Part Number | Yes |
| `tr_prod_line` | Transaction Product Line | Yes |
| `tr_qty_chg` | Quantity Change | Yes |
| `tr_qty_loc` | Local Quantity Change | Yes |
| `tr_qty_req` | Required Quantity | Yes |
| `tr_qty_short` | Short Quanitty | Yes |
| `tr_ref` | Reference | Yes |
| `tr_rmks` | Transaction Remarks | Yes |
| `tr_rsn_code` | Reason Code | Yes |
| `tr_serial` | Lot/Serial Number | Yes |
| `tr_ship_date` | Ship Date | Yes |
| `tr_ship_id` | Shipper  Number | Yes |
| `tr_ship_inv_mov` | Inventory Movement Code | Yes |
| `tr_ship_type` | Ship Type | Yes |
| `tr_site` | Transaction Site | Yes |
| `tr_slspsn##1` | Salesperson #1 - Scheduler | Yes |
| `tr_slspsn##2` | Salesperson #2 | Yes |
| `tr_slspsn##3` | Salesperson #3 | Yes |
| `tr_slspsn##4` | Salesperson #4 - Accounting Tech | Yes |
| `tr_so_job` | Sales Order Number | Yes |
| `tr_status` | Inventory Status | Yes |
| `tr_trnbr` | Transaction Number | Yes |
| `tr_type` | Transaction Type Code | Yes |
| `tr_um` | Unit of Measure | Yes |
| `tr_userid` | Transaction conducted by | Yes |
| `tr_vend_lot` | Supplier Lot Number | Yes |
| `tr_wod_op` | Operation | Yes |

### Table: `vp_mstr`

- **Business name:** Supplier Item Master
- **Source sheet:** `QADPRO2`
- **Fields:** 25

| Field | Description | Validated |
|---|---|---:|
| `vp__chr01` | Disqualified | Yes |
| `vp__chr03` | Quote Number | Yes |
| `vp__chr04` | Country of Origin | Yes |
| `vp__chr05` | NCNR (T/F) | Yes |
| `vp__dec01` | Reach Certificate Status | Yes |
| `vp__dte01` | Certificate of Origin Effective Date | Yes |
| `vp__dte02` | Certificate of Origin Requested | Yes |
| `vp__log01` | Multiple Country of Origin (T/F) | Yes |
| `vp_appr_date` | Approved Date | Yes |
| `vp_comment` | Comments | Yes |
| `vp_curr` | Currency | Yes |
| `vp_domain` | Domain | Yes |
| `vp_ins_rqd` | Inspection Required (T/F) | Yes |
| `vp_mfgr` | Manufacturer | Yes |
| `vp_mfgr_part` | Manufacturer Part Number | Yes |
| `vp_mod_date` | Modified Date | Yes |
| `vp_part` | Item Number | Yes |
| `vp_pr_list` | Price List | Yes |
| `vp_q_price` | AVL Price | Yes |
| `vp_um` | Unit of Measure | Yes |
| `vp_user1` | Manufacturing ID | Yes |
| `vp_user2` | Media Type | Yes |
| `vp_vend` | Supplier | Yes |
| `vp_vend_lead` | Supplier Lead Time | Yes |
| `vp_vend_part` | Supplier Part Number | Yes |

### Table: `wo_mstr`

- **Business name:** Work Order Master
- **Source sheet:** `QADPRO2`
- **Fields:** 40

| Field | Description | Validated |
|---|---|---:|
| `wo__chr01` | Build Area | Yes |
| `wo__chr04` | ID/PO Cross Reference | Yes |
| `wo__dec01` | MS Runnbr | Yes |
| `wo__dec02` | Est Completion Date | Yes |
| `wo__dte02` | Estimated Completion Date | Yes |
| `wo_batch` | Batch | Yes |
| `wo_bdn_tot` | Burden Cost Total | Yes |
| `wo_bom_code` | BOM Code | Yes |
| `wo_cmtindx` | Comment Index Number | Yes |
| `wo_doc_id` | Document ID | Yes |
| `wo_domain` | Domain | Yes |
| `wo_draw` | Drawing Number | Yes |
| `wo_due_date` | Due Date | Yes |
| `wo_eng_code` | Engineering Code | Yes |
| `wo_lead_time` | Work Order Lead Time | Yes |
| `wo_line` | Production Line | Yes |
| `wo_loc` | Work Order Location | Yes |
| `wo_lot` | Work Order ID | Yes |
| `wo_lot_next` | Lot/Serial | Yes |
| `wo_nbr` | Work Order Number | Yes |
| `wo_ord_date` | Order Date | Yes |
| `wo_ovh_tot` | Overhead Cost Total | Yes |
| `wo_part` | Item Number | Yes |
| `wo_qty_comp` | Quanity Complete | Yes |
| `wo_qty_ord` | Quantity Ordered | Yes |
| `wo_qty_rjct` | Quantity Rejected | Yes |
| `wo_rel_date` | Release Date | Yes |
| `wo_rmks` | Remarks | Yes |
| `wo_routing` | Routing Code | Yes |
| `wo_seq` | Sequence | Yes |
| `wo_shift` | Shift | Yes |
| `wo_site` | Site | Yes |
| `wo_so_job` | Sales Order/Job | Yes |
| `wo_status` | Work Order Status | Yes |
| `wo_type` | Type | Yes |
| `wo_user1` | WO Created By | Yes |
| `wo_user2` | NPI/PROTO Flag | Yes |
| `wo_vend` | Supplier | Yes |
| `wo_wip_tot` | WIP Cost | Yes |
| `wo_yield_pct` | Work Order Yield Percentage | Yes |

### Table: `wod_det`

- **Business name:** Work Order Detail
- **Source sheet:** `QADPRO2`
- **Fields:** 20

| Field | Description | Validated |
|---|---|---:|
| `wod_bom_amt` | Unit Cost | Yes |
| `wod_bom_qty` | Quantity Per Unit | Yes |
| `wod_cmtindx` | Comment Index Number | Yes |
| `wod_critical` | Key Item | Yes |
| `wod_domain` | Domain | Yes |
| `wod_iss_date` | Issue Date | Yes |
| `wod_loc` | Location | Yes |
| `wod_lot` | Work Order ID | Yes |
| `wod_nbr` | Work Order Number | Yes |
| `wod_op` | Operation Number | Yes |
| `wod_part` | Component Item | Yes |
| `wod_prod_line` | Product Line | Yes |
| `wod_qty_all` | Quantity Allocated | Yes |
| `wod_qty_iss` | Quantity Issued | Yes |
| `wod_qty_pick` | Quantity Picked | Yes |
| `wod_qty_req` | Quantity Required | Yes |
| `wod_site` | Site | Yes |
| `wod_sod_line` | Work Order Sales Order Line | Yes |
| `wod_sod_nbr` | Work Order Sales Order Number | Yes |
| `wod_status` | Status | Yes |

## Database: Analysis

### Table: `in_price`

- **Business name:** in_price
- **Source sheet:** `Analysis`
- **Fields:** 7

| Field | Description | Validated |
|---|---|---:|
| `inp_list_id` | Price List ID Number | Yes |
| `inp_start_date` | Price List Start Date | Yes |
| `inp_part` | Part Number | Yes |
| `inp_domain` | Domain | Yes |
| `inp_custprice` | Customer's Price | Yes |
| `inp_qctc` | Quoted Cost — accepted Stage 8D.5 Component Detail QCTC source, filtered to `inp_source = 'qtbom_det'` (other sources such as `idh_hist`/`pid_det` were observed to always carry `inp_qctc = 0` and can share the same latest `inp_start_date` as a real `qtbom_det` row) | Yes |
| `inp_site` | Site | Yes |
