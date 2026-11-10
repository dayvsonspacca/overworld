use std::env;
use std::path::Path;
use std::process::ExitCode;
use std::thread;
use std::time::Duration;

use tauri::image::Image;
use tauri::{Url, WebviewUrl, WebviewWindowBuilder};

struct Wanted {
    parent: Option<u32>,
    app: String,
    title: String,
    url: Url,
    icon: Option<String>,
}

fn wanted() -> Result<Wanted, String> {
    let mut app = None;
    let mut title = None;
    let mut url = None;
    let mut icon = None;
    let mut parent = None;
    let mut args = env::args().skip(1);

    while let Some(arg) = args.next() {
        let value = args.next().ok_or(format!("{arg} needs a value"))?;
        match arg.as_str() {
            "--app" => app = Some(value),
            "--title" => title = Some(value),
            "--url" => url = Some(Url::parse(&value).map_err(|e| format!("--url: {e}"))?),
            "--icon" => icon = Some(value),
            "--parent" => parent = Some(value.parse().map_err(|e| format!("--parent: {e}"))?),
            _ => return Err(format!("unknown option {arg}")),
        }
    }

    let app = app.ok_or("--app is required")?;
    Ok(Wanted {
        title: title.unwrap_or_else(|| app.clone()),
        url: url.ok_or("--url is required")?,
        icon,
        parent,
        app,
    })
}

fn main() -> ExitCode {
    let wanted = match wanted() {
        Ok(wanted) => wanted,
        Err(problem) => {
            eprintln!("overworld-glass: {problem}");
            eprintln!("usage: overworld-glass --app <name> --url <url> [--title <title>] [--icon <png>] [--parent <pid>]");
            return ExitCode::from(2);
        }
    };

    if let Some(parent) = wanted.parent {
        thread::spawn(move || loop {
            thread::sleep(Duration::from_millis(500));
            if !Path::new(&format!("/proc/{parent}")).exists() {
                std::process::exit(0);
            }
        });
    }

    let input = env::var("OVERWORLD_IM_MODULE").unwrap_or_else(|_| "gtk-im-context-simple".into());
    env::set_var("GTK_IM_MODULE", input);

    gtk::glib::set_prgname(Some(wanted.app.as_str()));
    gtk::glib::set_application_name(&wanted.title);

    let result = tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .setup(move |app| {
            let window = WebviewWindowBuilder::new(app, "main", WebviewUrl::External(wanted.url.clone()))
                .title(&wanted.title)
                .inner_size(960.0, 640.0)
                .min_inner_size(480.0, 320.0)
                .decorations(false)
                .build()?;

            if let Some(icon) = &wanted.icon {
                if let Ok(image) = Image::from_path(icon) {
                    let _ = window.set_icon(image);
                }
            }

            Ok(())
        })
        .run(tauri::generate_context!());

    match result {
        Ok(()) => ExitCode::SUCCESS,
        Err(problem) => {
            eprintln!("overworld-glass: {problem}");
            ExitCode::FAILURE
        }
    }
}
